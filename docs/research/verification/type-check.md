# Type-checking against Unity's own DLLs

How we compile the project the way the Unity editor does, without Unity and without a licence. We use the compiler and reference assemblies from the same GameCI image CI builds with. `tools/check.sh` does it all; this is why and how.

**Read this when** a compile error needs explaining, you are upgrading Unity or an SDK, or a CI compile error needs reproducing.

Up: [Verification](README.md)

## 1. Where the compiler and DLLs come from

- **A prebuilt Unity kit.** The vendor repository's *Unity kit* workflow streams the GameCI editor image's editor layer (about 7 GB), keeps only what type-checking needs, builds uGUI and the Input System from the editor's bundled sources, and publishes the result as a release. [`tools/check.sh`](../../../tools/check.sh) downloads it once per Unity version (the version is in `ProjectSettings/ProjectVersion.txt`) and caches it. Agents never run the extraction scripts ([tools](tools/README.md)).
- **What the kit holds:**

  | Part | Why |
  |---|---|
  | `UnityEngine.*.dll`, `UnityEditor*.dll` | Reference assemblies, one per engine module, plus the editor APIs. Each has an XML doc file, which is handy for checking an API exists |
  | `Unity.CompilationPipeline.Common.dll` | The ILPostProcessor contract, for the [weaver harness](fusion-weaver.md) |
  | `netstandard.dll` | The base class library the scripts compile against |
  | Unity's Roslyn (`csc.dll`) and the .NET runtime it runs on | The compiler, as the editor uses it |
  | Built-in package metadata | Module dependencies, used to work out which modules are enabled |
  | `UnityEngine.UI.dll`, `Unity.InputSystem.dll`, Cecil, NUnit | Built from the editor's bundled packages |

- **After a Unity upgrade,** run the vendor repository's *Unity kit* workflow with the new version first, or `check.sh` stops with an error saying so.

## 2. Compile like the editor

- **Compiler flags:** `csc.dll` run on the bundled `dotnet`, with `-langversion:9.0 -nostdlib -noconfig -target:library`, `netstandard.dll`, and the engine module DLLs as references.
- **Defines:** `UNITY_6000_3_OR_NEWER` and friends, `UNITY_WEBGL`, plus the input defines.
- **uGUI and the Input System's UI support** come from the kit, built together as Unity does once uGUI is in the project. They are passed to `compile_unity.py` as `--ugui` and `--inputsystem`.
- **Check both input backends.** The active one isn't known until Unity generates ProjectSettings ([why](../build-pipeline/unity-project.md#input)). Compile once with `ENABLE_LEGACY_INPUT_MANAGER`, once with `ENABLE_INPUT_SYSTEM` (referencing `Unity.InputSystem.dll`), and once with both: `compile_unity.py --input legacy`, `--input new`, and the default, `--input both`.
- **Then the editor assembly:** compile `Assets/Editor/**` against the game assembly plus `UnityEditor*.dll`, with `UNITY_EDITOR` defined.
- **Assembly definitions first.** Each `.asmdef` outside `Assets/Photon` compiles the scripts under its folder, after the ones it references. `Assembly-CSharp` gets everything else and references the auto-referenced ones, such as `PooketSlug.Core`.
  - The netcode's `PooketSlug.Net` references the SDK's `Fusion.Unity` and `Photon.Realtime`, plus its auto-referenced DLLs, as in Unity. Without the SDK it's skipped, because `FUSION2` isn't defined.
  - A test assembly compiles only when its define constraint, `UNITY_INCLUDE_TESTS`, holds. The Test Framework defines it in the editor.
  - It also needs `--nunit` ([EditMode tests](edit-mode-tests.md)).
- [`compile_unity.py`](tools/compile_unity.py) does all of this: the `game` stage, or `all` for the SDK and the game.

## Emulating disabled modules

- **Unity only gives scripts the `UnityEngine.<X>Module.dll` of modules the manifest enables,** directly or through a package's dependencies. A missing module is a compile error *in CI only*.
- **`compile_unity.py` emulates this:**
  1. Reads `Packages/manifest.json`.
  2. Expands dependencies from each built-in package's `package.json`.
  3. Leaves out the DLLs of every `com.unity.modules.*` package that isn't enabled.
- **This found real problems before CI did.** Compiling the Photon SDK this way showed it needs:
  - `unitywebrequest` (`PhotonPing.cs`)
  - `imageconversion` (`Texture2D.LoadImage` in Fusion's editor)
  - `physics`, `physics2d`, `ai` and `animation` (Fusion.Unity and its editor)

  The full list is in [Unity project](../build-pipeline/unity-project.md#packages-and-engine-modules).

## Compiling the Input System and the Photon SDK

- **Input System:** the kit has it built from the bundled package's runtime sources, without the `Editor/` folders and the `InputForUI` plugin (it uses Unity-internal APIs).
- **Photon SDK:** with the SDK unpacked into `Assets/Photon/`, the `sdk` stage compiles each asmdef as its own assembly, for the player and for the editor. `Unity.Fusion.CodeGen` is compiled with `FUSION_WEAVER_ILPOSTPROCESSOR`, as its asmdef's version-defines would set it:

  | Assembly | Notes |
  |---|---|
  | `Photon.Realtime` | |
  | `PhotonWebSocket` | WebGL only, needs `websocket-sharp.dll` |
  | `Fusion.Unity` | `-unsafe`, `FUSION_FORCE_WEAVING` |
  | `Fusion.Unity.Editor` | Needs Cecil |
  | `Unity.Fusion.CodeGen` | The weaver. Needs Cecil and `FUSION_WEAVER` |

- **The game** then compiles with the Fusion defines (`FUSION_WEAVER`, `FUSION2`, …) against `Fusion.*.dll`, `PhotonClient.dll` and the two compiled SDK assemblies.

## Pitfalls we hit

- **`InitializeOnLoadAttribute` came out ambiguous** when referencing both the module DLLs and older facades. Reference `Managed/UnityEngine/UnityEngine.dll` (the facade with type forwarders) together with the module DLLs, not the `Managed/UnityEngine.dll` next to it.
- **Name each output DLL after the assembly** (`Fusion.Unity.dll`, not `fusion-unity.dll`). Attributes like `InternalsVisibleTo` and later references match on assembly names.
- **An unquoted `;` in a `-define:` list breaks shell commands.** Quote the whole argument.
- **`CS0012: The type 'Attribute' is defined in an assembly that is not referenced … 'mscorlib'`** came from compiling against Unity's NUnit, which targets .NET Framework. The editor also references the facades in `NetStandard/compat/2.1.0/shims/netfx/`, and now `compile_unity.py` does too.
- **These compiles don't run the weaver.** Networked properties compile as plain auto-properties until [woven](fusion-weaver.md).

## Not covered

- Unity-only checks: serialization rules, `[RuntimeInitializeOnLoadMethod]` ordering, IL2CPP code generation and stripping.
- Platform behaviour.

For those, the CI build is the test, and its `[PooketSlug]` diagnostics line plus a [smoke test](browser-tests.md) confirm the result.
