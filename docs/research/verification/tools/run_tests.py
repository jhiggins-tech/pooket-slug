"""Run the EditMode tests outside Unity, with NUnitLite on the .NET runtime inside the extracted editor.

compile_unity.py type-checks the test assemblies against Unity's own NUnit (a custom NUnit 3.5), as the
editor does. This script runs them: it compiles the same test sources against NuGet's NUnit 3.14 with an
NUnitLite entry point, then runs that on the editor's bundled .NET runtime. Only pure code runs here.
Anything that calls into the engine's native side (Debug.Log, GameObject, ...) throws, which is one
reason the tested code lives in the PooketSlug.Core assembly. CI runs the same tests inside Unity.

usage: python3 -I run_tests.py --editor-data <.../Editor/Data> --project <repo> --core <PooketSlug.Core.dll>
                               --nunit <dir> --nunitlite <dir> --out <dir> [NUnitLite options]
  --nunit, --nunitlite   lib/netstandard2.0 folders of the NuGet packages nunit and nunitlite 3.14.0
  NUnitLite options      passed through, such as --where "class == PooketSlug.Tests.RngTests"
Exits with NUnitLite's code: the number of failed tests, or negative if the run itself failed.
"""
import argparse, glob, json, os, shutil, subprocess, sys

ap = argparse.ArgumentParser()
ap.add_argument("--editor-data", required=True, help="extracted .../Editor/Data folder")
ap.add_argument("--project", required=True, help="Unity project root")
ap.add_argument("--core", required=True, help="PooketSlug.Core.dll from compile_unity.py's game stage")
ap.add_argument("--nunit", required=True, help="folder with NuGet's nunit.framework.dll")
ap.add_argument("--nunitlite", required=True, help="folder with NuGet's nunitlite.dll")
ap.add_argument("--out", required=True, help="output folder")
a, nunitlite_args = ap.parse_known_args()

D, OUT = a.editor_data, a.out
DOTNET, CSC = os.path.join(D, "NetCoreRuntime/dotnet"), os.path.join(D, "DotNetSdkRoslyn/csc.dll")
RT = sorted(glob.glob(os.path.join(D, "NetCoreRuntime/shared/Microsoft.NETCore.App/*")))[-1]
os.makedirs(OUT, exist_ok=True)

# The test sources plus an NUnitLite Main, built against the runtime's own assemblies (as weave.sh does).
main = os.path.join(OUT, "TestMain.cs")
with open(main, "w") as f:
    f.write("public static class TestMain\n{\n    public static int Main(string[] args) =>\n"
            "        new NUnitLite.AutoRun(typeof(TestMain).Assembly).Execute(args);\n}\n")
tests = sorted(glob.glob(os.path.join(a.project, "Assets/Tests/EditMode/**/*.cs"), recursive=True))
deps = [a.core, os.path.join(D, "Managed/UnityEngine/UnityEngine.CoreModule.dll"),
        os.path.join(a.nunit, "nunit.framework.dll"), os.path.join(a.nunitlite, "nunitlite.dll")]
runtime = [p for p in sorted(glob.glob(os.path.join(RT, "*.dll"))) if not os.path.basename(p).startswith("Microsoft.VisualBasic")]
exe = os.path.join(OUT, "EditModeTests.dll")
r = subprocess.run([DOTNET, CSC, "-nologo", "-noconfig", "-nostdlib", "-langversion:9.0", "-target:exe", "-nowarn:1701,1702",
                    "-out:" + exe] + ["-r:" + p for p in runtime + deps] + tests + [main], capture_output=True, text=True)
errors = [l for l in (r.stdout + r.stderr).splitlines() if " error " in l or " warning " in l]
print(f"runner: {len(tests)} test files -> {'OK' if r.returncode == 0 else 'FAILED'}")
for l in errors[:30]: print("   ", l.replace(a.project + "/", ""))
if r.returncode != 0:
    sys.exit(1)

# Run it from its own folder, where the dependencies sit (a folder without .deps.json is all on the path).
for p in deps: shutil.copy(p, OUT)
with open(os.path.join(OUT, "EditModeTests.runtimeconfig.json"), "w") as f:
    json.dump({"runtimeOptions": {"tfm": "net6.0", "framework": {"name": "Microsoft.NETCore.App", "version": os.path.basename(RT)}}}, f)
sys.exit(subprocess.run([DOTNET, exe, "--noheader", "--result=" + os.path.join(OUT, "TestResult.xml")] + nunitlite_args).returncode)
