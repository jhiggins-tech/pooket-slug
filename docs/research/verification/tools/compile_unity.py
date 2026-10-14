"""Type-check a Unity project the way the editor compiles it, without Unity or a license.

Uses the Roslyn compiler and reference assemblies extracted from the GameCI editor image (see
extract-unity.md), C# 9 like the editor, and only the engine modules Packages/manifest.json enables:
Unity leaves UnityEngine.<X>Module.dll out of the compile when a com.unity.modules.* package is absent.

Stages:
  sdk   compile Photon's SDK sources from Assets/Photon (Photon.Realtime, PhotonWebSocket, Fusion.Unity,
        Fusion.Unity.Editor, Unity.Fusion.CodeGen) for the WebGL player and the editor
  game  compile the game's Assembly-CSharp (and Assembly-CSharp-Editor) against the SDK, with a portable
        PDB, plus the reference and define lists the weaver harness needs (see weave-harness.md)
  all   both (default)

usage: python3 -I compile_unity.py --editor-data <.../Editor/Data> --project <repo> --out <dir>
                                   [--cecil-dir <dir>] [--inputsystem <Unity.InputSystem.dll>] [sdk|game|all]
"""
import argparse, glob, json, os, subprocess, sys

ap = argparse.ArgumentParser()
ap.add_argument("--editor-data", required=True, help="extracted .../Editor/Data folder")
ap.add_argument("--project", required=True, help="Unity project root (Assets/Photon unpacked for the SDK stages)")
ap.add_argument("--out", required=True, help="output folder")
ap.add_argument("--cecil-dir", help="folder with Mono.Cecil*.dll (com.unity.nuget.mono-cecil package), for the editor SDK assemblies")
ap.add_argument("--inputsystem", help="a compiled Unity.InputSystem.dll, if the game uses the Input System")
ap.add_argument("stage", nargs="?", default="all", choices=["sdk", "game", "all"])
a = ap.parse_args()

D, PROJ, OUT = a.editor_data, a.project, a.out
MANAGED = os.path.join(D, "Managed/UnityEngine")
CSC = [os.path.join(D, "NetCoreRuntime/dotnet"), os.path.join(D, "DotNetSdkRoslyn/csc.dll")]
BUILTIN = os.path.join(D, "Resources/PackageManager/BuiltInPackages")
PH = os.path.join(PROJ, "Assets/Photon")
CECIL = glob.glob(os.path.join(a.cecil_dir, "Mono.Cecil*.dll")) if a.cecil_dir else []
EXTRA = [a.inputsystem] if a.inputsystem else []
NETSTD = os.path.join(D, "NetStandard/ref/2.1.0/netstandard.dll")

# Enabled modules = manifest dependencies plus their transitive built-in dependencies.
manifest = json.load(open(os.path.join(PROJ, "Packages/manifest.json")))["dependencies"]
enabled, todo = set(), list(manifest)
implied = {"com.unity.inputsystem": ["com.unity.modules.uielements"]}
while todo:
    p = todo.pop()
    if p in enabled:
        continue
    enabled.add(p)
    pj = os.path.join(BUILTIN, p, "package.json")
    todo += list(json.load(open(pj)).get("dependencies", {}) if os.path.exists(pj) else []) + implied.get(p, [])
modules = {d[len("com.unity.modules."):] for d in os.listdir(BUILTIN) if d.startswith("com.unity.modules.")}
module_of = lambda dll: os.path.basename(dll)[len("UnityEngine."):-len("Module.dll")].lower()
engine = [d for d in sorted(glob.glob(os.path.join(MANAGED, "UnityEngine.*Module.dll")))
          if module_of(d) not in modules or "com.unity.modules." + module_of(d) in enabled]
engine.append(os.path.join(MANAGED, "UnityEngine.dll"))  # facade with type forwarders
editor = sorted(glob.glob(os.path.join(MANAGED, "UnityEditor.*.dll"))) + [
    os.path.join(MANAGED, "UnityEditor.dll"), os.path.join(D, "Managed/Unity.CompilationPipeline.Common.dll")]
print("modules left out:", sorted(module_of(d) for d in glob.glob(os.path.join(MANAGED, "UnityEngine.*Module.dll"))
                                   if module_of(d) in modules and "com.unity.modules." + module_of(d) not in enabled))

version = ["UNITY_%d_%d_OR_NEWER" % (y, m) for y in range(2017, 2024) for m in range(1, 5)] + \
          ["UNITY_6000_%d_OR_NEWER" % m for m in range(0, 4)]
fusion = ["FUSION_WEAVER", "FUSION2", "FUSION_2", "FUSION_2_1", "FUSION_2_1_3", "FUSION_LOGLEVEL_INFO",
          "FUSION_HAS_MONO_CECIL", "FUSION_CECIL_1_11_OR_NEWER"]  # what Fusion's installer and asmdefs add
common = version + ["UNITY_6000_3", "UNITY_6000", "NET_STANDARD_2_1", "NET_STANDARD", "CSHARP_7_3_OR_NEWER",
                    "ENABLE_INPUT_SYSTEM", "ENABLE_LEGACY_INPUT_MANAGER"] + fusion
player = common + ["UNITY_WEBGL", "PLATFORM_WEBGL", "ENABLE_IL2CPP"]
editdefs = common + ["UNITY_EDITOR", "UNITY_EDITOR_64", "UNITY_EDITOR_LINUX", "ENABLE_MONO", "UNITY_WEBGL"]

FUSION_DLLS = [os.path.join(PH, "Fusion/Assemblies/%s.dll" % n) for n in ["Fusion.Runtime", "Fusion.Common", "Fusion.Sockets", "Fusion.Realtime", "Fusion.Log"]]
PHOTONCLIENT = os.path.join(PH, "PhotonLibs/netstandard2.0/release/PhotonClient.dll")
WSS = os.path.join(PH, "PhotonLibs/WebSocket/websocket-sharp.dll")
SDK_NOWARN = "1701,1702,0618,0649,0414,0169,0067,0162,0168,0219,8321"

def sources(folder, keep=lambda f: True):
    return sorted(f for f in glob.glob(os.path.join(folder, "**/*.cs"), recursive=True) if keep(f))

def csc(name, srcs, refs, defines, ctx, unsafe=False, nowarn=SDK_NOWARN, debug=False, show_warnings=False):
    os.makedirs(os.path.join(OUT, ctx), exist_ok=True)
    out = os.path.join(OUT, ctx, name + ".dll")
    args = CSC + ["-nologo", "-noconfig", "-nostdlib", "-langversion:9.0", "-target:library", "-nowarn:" + nowarn,
                  "-define:" + ";".join(defines), "-out:" + out, "-r:" + NETSTD] + ["-r:" + r for r in refs] + srcs
    if unsafe: args.insert(2, "-unsafe")
    if debug: args[2:2] = ["-debug:portable", "-pdb:" + out[:-4] + ".pdb"]
    r = subprocess.run(args, capture_output=True, text=True)
    lines = [l for l in (r.stdout + r.stderr).splitlines() if " error " in l or (show_warnings and " warning " in l)]
    print(f"[{ctx}] {name}: {len(srcs)} files -> {'OK' if r.returncode == 0 else 'FAILED'}")
    for l in lines[:30]: print("   ", l.replace(PROJ + "/", ""))
    return out if r.returncode == 0 else None

ok = True
if a.stage in ("sdk", "all"):
    rt = csc("Photon.Realtime", sources(os.path.join(PH, "PhotonRealtime")), engine + [PHOTONCLIENT], player, "player")
    ws = csc("PhotonWebSocket", sources(os.path.join(PH, "PhotonLibs/WebSocket")), engine + [PHOTONCLIENT, WSS], player, "player")
    fu = rt and csc("Fusion.Unity", sources(os.path.join(PH, "Fusion/Runtime")), engine + FUSION_DLLS + [PHOTONCLIENT, rt], player + ["FUSION_FORCE_WEAVING"], "player", unsafe=True)
    rte = csc("Photon.Realtime", sources(os.path.join(PH, "PhotonRealtime")), engine + editor + [PHOTONCLIENT], editdefs, "editor")
    fue = rte and csc("Fusion.Unity", sources(os.path.join(PH, "Fusion/Runtime")), engine + editor + FUSION_DLLS + [PHOTONCLIENT, rte], editdefs + ["FUSION_FORCE_WEAVING"], "editor", unsafe=True)
    fed = fue and csc("Fusion.Unity.Editor", sources(os.path.join(PH, "Fusion/Editor")), engine + editor + FUSION_DLLS + CECIL + [PHOTONCLIENT, rte, fue], editdefs, "editor", unsafe=True)
    cg = csc("Unity.Fusion.CodeGen", sources(os.path.join(PH, "Fusion/CodeGen")), engine + editor + FUSION_DLLS + CECIL, editdefs + ["FUSION_WEAVER_ILPOSTPROCESSOR"], "editor", unsafe=True)
    ok = all([rt, ws, fu, rte, fue, fed, cg])
    print("SDK", "OK" if ok else "FAILED")

if a.stage in ("game", "all") and ok:
    sdk_player = [os.path.join(OUT, "player", n) for n in ("Photon.Realtime.dll", "Fusion.Unity.dll")]
    sdk_editor = [os.path.join(OUT, "editor", n) for n in ("Photon.Realtime.dll", "Fusion.Unity.dll")]
    has_sdk = os.path.isdir(PH) and all(os.path.exists(p) for p in sdk_player)
    game_defs = player if has_sdk else [d for d in player if not d.startswith("FUSION")]
    refs = engine + EXTRA + ((FUSION_DLLS + [PHOTONCLIENT] + sdk_player) if has_sdk else [])
    keep = lambda f: "/Assets/Photon/" not in f
    asm = csc("Assembly-CSharp", sources(os.path.join(PROJ, "Assets"), lambda f: keep(f) and "/Editor/" not in f),
              refs, game_defs, "game", nowarn="1701,1702", debug=True, show_warnings=True)
    if asm:
        with open(os.path.join(OUT, "game", "Assembly-CSharp.refs.txt"), "w") as f: f.write("\n".join([NETSTD] + refs) + "\n")
        with open(os.path.join(OUT, "game", "Assembly-CSharp.defines.txt"), "w") as f: f.write(";".join(game_defs))
        ed_refs = engine + editor + EXTRA + ((FUSION_DLLS + [PHOTONCLIENT] + sdk_editor) if has_sdk else []) + [asm]
        ed_defs = editdefs if has_sdk else [d for d in editdefs if not d.startswith("FUSION")]
        ok = bool(csc("Assembly-CSharp-Editor", sources(os.path.join(PROJ, "Assets"), lambda f: keep(f) and "/Editor/" in f),
                      ed_refs, ed_defs, "game", nowarn="1701,1702", show_warnings=True))
    else:
        ok = False
sys.exit(0 if ok else 1)
