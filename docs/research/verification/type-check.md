# Type-checking against Unity's own DLLs

How we compile the project the way the Unity editor does, without Unity and without a license. We use the compiler and reference assemblies from the same GameCI image CI builds with.

**Read this when** you want to check C# before pushing, are upgrading Unity or an SDK, or a CI compile error needs reproducing.

Up: [Verification](README.md)

## 1. Get Unity's compiler and DLLs from the editor image

- **The `unityci/editor` image is ~7 GB compressed, and Docker isn't needed.** [`extract-unity.sh`](tools/extract-unity.sh) does it with plain HTTP:
  1. Asks Docker Hub for an anonymous pull token.
  2. Resolves the `linux/amd64` manifest of the tag (for example `ubuntu-6000.3.26f1-webgl-3`).
  3. Streams its **largest layer**, the editor: 7.12 GB for 6000.3.26f1.
  4. Pipes it through `tar -xz --wildcards`, so only the parts below reach the disk.
- **What it keeps:**

  | Path under `Editor/Data` | Why |
  |---|---|
  | `Managed/UnityEngine/UnityEngine.*.dll`, `UnityEditor*.dll` | Reference assemblies, one per engine module, plus the editor APIs. Each has an XML doc file, which is handy for checking an API exists |
  | `Managed/Unity.CompilationPipeline.Common.dll` | The ILPostProcessor contract, for the [weaver harness](fusion-weaver.md) |
  | `NetStandard/ref/2.1.0/netstandard.dll` | The base class library the scripts compile against |
  | `DotNetSdkRoslyn/csc.dll`, `NetCoreRuntime/` | Unity's own Roslyn compiler and the .NET 6 runtime it runs on |
  | `Resources/PackageManager/BuiltInPackages/*/package.json` | Module dependencies, used to work out which modules are enabled |
  | `Resources/PackageManager/Editor/com.unity.inputsystem-*.tgz`, `com.unity.nuget.mono-cecil-*.tgz` | The package versions bundled with this editor |
  | `PlaybackEngines/WebGLSupport/BuildTools/WebGLTemplates/` | Unity's stock templates, for reference when editing ours |

- **Rate limit.** Docker Hub allows **100 anonymous manifest requests per hour per IP.** The cloud sandbox's shared egress IP ran out on 2026-10-14 (HTTP 429). Authenticate with `DOCKER_USER` / `DOCKER_TOKEN` if that happens.

## 2. Compile like the editor

- **Compiler flags:** `csc.dll` run on the bundled `dotnet`, with `-langversion:9.0 -nostdlib -noconfig -target:library`, `netstandard.dll`, and the engine module DLLs as references.
- **Defines:** `UNITY_6000_3_OR_NEWER` and friends, `UNITY_WEBGL`, plus the input defines.
- **Check both input backends.** The active one isn't known until Unity generates ProjectSettings ([why](../build-pipeline/unity-project.md#input)). Compile once with `ENABLE_LEGACY_INPUT_MANAGER`, once with `ENABLE_INPUT_SYSTEM` (referencing `Unity.InputSystem.dll`), and once with both.
- **Then the editor assembly:** compile `Assets/Editor/**` against the game assembly plus `UnityEditor*.dll`, with `UNITY_EDITOR` defined.
- [`compile_unity.py`](tools/compile_unity.py) does all of this: the `game` stage, or `all` for the SDK and the game.
- **Track record:** zero errors and zero warnings before the first push of the game, and no compile failures in CI since.

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

- **Input System:** [`build-inputsystem.sh`](tools/build-inputsystem.sh) compiles the bundled package's runtime sources into `Unity.InputSystem.dll`. That's 281 files, everything except `Editor/` folders and the `InputForUI` plugin, which uses Unity-internal APIs.
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
- **These compiles don't run the weaver.** Networked properties compile as plain auto-properties until [woven](fusion-weaver.md).

## Not covered

- Unity-only checks: serialization rules, `[RuntimeInitializeOnLoadMethod]` ordering, IL2CPP code generation and stripping.
- Platform behaviour.

For those, the CI build is the test, and its `[PooketSlug]` diagnostics line plus a [smoke test](browser-tests.md) confirm the result.
