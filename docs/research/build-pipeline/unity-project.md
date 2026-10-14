# The Unity project and build script

How the Unity side is set up: the version and editor image, a project that's almost entirely code, the packages it uses, input handling, and what [`WebGLBuild.cs`](../../../Assets/Editor/WebGLBuild.cs) does.

**Read this when** you're upgrading Unity, adding a package or module, changing PlayerSettings, or wondering where a generated file comes from.

Up: [Build pipeline](README.md)

## Version and editor image

- **Unity 6000.3.26f1 (6.3 LTS),** pinned in [`ProjectSettings/ProjectVersion.txt`](../../../ProjectSettings/ProjectVersion.txt). At runtime it reports engine revision `6000.3.26f1 (de8167e7440c)`.
- **GameCI image:** `unityci/editor:ubuntu-6000.3.26f1-webgl-3`. Its editor layer alone is 7.1 GB compressed.
- **Check the image exists before pinning a version.** Look for `ubuntu-<version>-webgl-3` on Docker Hub, and don't assume a brand-new patch release already has one:

  ```sh
  curl -s "https://hub.docker.com/v2/repositories/unityci/editor/tags?page_size=100&name=ubuntu-6000.3" \
    | python3 -c "import sys,json; [print(r['name']) for r in json.load(sys.stdin)['results'] if r['name'].endswith('webgl-3')]"
  ```

  When we checked (October 2026), the 6000.0 and 6000.3 LTS lines and the 6000.4–6000.6 tech-stream lines all had WebGL images.
- **Scripts compile as C# 9,** like the editor. `-langversion:9.0` is what the [type-check harness](../verification/type-check.md) uses.

## A code-only project

The committed Unity project is small:

| Path | What it is |
|---|---|
| `ProjectSettings/ProjectVersion.txt` | The only committed ProjectSettings file. Unity generates the rest with defaults on every CI run |
| `Packages/manifest.json` | Packages and engine modules (see below) |
| `Assets/Scripts/**.cs`, `Assets/Editor/WebGLBuild.cs` | All gameplay, art, audio and UI, plus the build entry point, each with its `.meta` |
| `Assets/Scripts/Core/` | The pure logic, in its own assembly, `PooketSlug.Core` (an `.asmdef` with `autoReferenced`, so the rest of the game sees it) |
| `Assets/Tests/EditMode/` | NUnit tests for Core, in an editor-only test assembly ([EditMode tests](../verification/edit-mode-tests.md)) |
| `Assets/WebGLTemplates/PooketSlug/index.html` | The page around the canvas ([GitHub Pages](github-pages.md#the-webgl-template)) |
| `Assets/link.xml` | Keeps `Assembly-CSharp` and `PooketSlug.Core` whole under managed code stripping. Everything is created from code at runtime, so no scene or prefab references keep types alive |
| `Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion` | Fusion's config: only `AssembliesToWeave` ([SDK in CI](../netcode/sdk-in-ci.md)) |

Everything else is generated:
- **The scene:** `Assets/Scenes/Main.unity` is created empty by the build script and git-ignored.
- **The game:** a `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` bootstrap in [`Game.cs`](../../../Assets/Scripts/Game.cs) spawns it into that scene.
- **Content:** sprites (ASCII art in `ArtData.cs`), tiles, parallax backgrounds and sound effects are all built at startup. There's no asset import pipeline.
- **Network objects:** built in code too ([architecture](../netcode/architecture.md#network-objects-without-prefabs)).

Why it's worth it:
- Nobody hand-writes Unity YAML.
- Diffs are reviewable.
- The project can be type-checked without Unity ([verification](../verification/README.md)).

The cost:
- Real content needs an asset pipeline eventually ([roadmap](../roadmap/README.md)).
- A few Unity defaults stay unknown until CI generates them. Active Input Handling is one of them, handled [below](#input).

## Packages and engine modules

`com.unity.modules.*` packages decide which `UnityEngine.<X>Module.dll` assemblies scripts can see. **A module that's missing from the manifest is missing at compile time.** We found out which modules the Photon SDK needs by compiling it against only the enabled ones ([type-check](../verification/type-check.md#emulating-disabled-modules)):

| Package | Needed by |
|---|---|
| `com.unity.inputsystem` 1.20.0 | `GameInput.cs` (keyboard and gamepad). Pinned to the version **bundled in the editor image** (`Editor/Data/Resources/PackageManager/Editor/`), so resolving packages needs no registry fetch |
| `com.unity.modules.audio`, `imgui`, `jsonserialize` | The game: procedural audio, the IMGUI HUD, and `JsonUtility` for `pooket-net.json` |
| `com.unity.modules.physics`, `physics2d` | Fusion.Unity (`RunnerSimulatePhysics`, `Rigidbody2D` helpers). The game itself uses its own tile physics |
| `com.unity.modules.ai` | Fusion.Unity and its editor (NavMesh helpers) |
| `com.unity.modules.animation` | Fusion's editor scripts (`Animator` tooling) |
| `com.unity.modules.unitywebrequest` | Photon Realtime's `PhotonPing.cs` and Fusion's editor |
| `com.unity.modules.imageconversion` | Fusion's editor (`Texture2D.LoadImage`) |
| `com.unity.test-framework` 1.6.0 | The EditMode tests. It's built into the editor (a core package), so it needs no registry fetch, and it brings `com.unity.ext.nunit` |
| `com.unity.nuget.mono-cecil` 1.11.6 | Fusion's IL weaver. This is the version bundled with the editor. Listing it also stops Fusion's installer from calling `Client.Add("com.unity.nuget.mono-cecil@1.10.2")` mid-build ([SDK in CI](../netcode/sdk-in-ci.md#fusions-defines-and-the-il-weaver)) |

## Input

- **Active Input Handling is unknown.** It isn't set until Unity generates ProjectSettings, so [`GameInput.cs`](../../../Assets/Scripts/GameInput.cs) supports both backends: `#if ENABLE_INPUT_SYSTEM` (keyboard and gamepad) and the legacy Input Manager (keyboard only).
- **Input is polled once per frame into a snapshot** (`MoveX`, `JumpPressed`, `HostPressed` and so on), which gameplay reads.
- **The Input System's "enable the new backends and restart?" dialog can't hang CI.** It's skipped in batch mode: `!Application.isBatchMode` in `InputSystemEditorInitializer.cs`.

## The build script

[`Assets/Editor/WebGLBuild.cs`](../../../Assets/Editor/WebGLBuild.cs) is GameCI's `buildMethod`. It's also on the menu at **Pooket Slug → Build WebGL**, which writes to `Builds/WebGL`. In order, it:

1. **Reads its arguments** with `Environment.GetCommandLineArgs()`: `-customBuildPath` (from GameCI), `-buildSha`, `-photonAppId` and `-photonRegion` (from `customParameters`). A flag whose value is missing or starts with `-` counts as absent.
2. **Writes the Photon config** to `Assets/Resources/pooket-net.json` (`{"appId": …, "region": …}`, git-ignored), then runs `AssetDatabase.Refresh()`.
   - Without `-photonAppId`, it logs that co-op is off and leaves any existing local file alone.
3. **Ensures a scene** at `Assets/Scenes/Main.unity` and sets it as the only build scene.
4. **Configures the player:**
   - `EditorSettings.serializationMode = ForceText`, which Fusion requires.
   - Company and product names.
   - `bundleVersion` = `0.2.0+<short sha>`, or `0.2.0-local`. This is the version shown in-game, and it's also Photon's `AppVersion` ([sessions](../netcode/sessions.md#build-versions)).
   - A 960×540 default canvas, `runInBackground`, no splash screen.
   - **WebGL:** compression `Disabled`, `decompressionFallback` off, `nameFilesAsHashes` off, `dataCaching` on, template `PROJECT:PooketSlug`.
5. **Runs `AssetDatabase.SaveAssets()`,** then **logs one `[PooketSlug]` diagnostics line:** SDK present, weaver compiled, co-op code compiled, WebGL defines.
6. **Builds** with `BuildPipeline.BuildPlayer`, prints a result summary and exits `0` or `101`.

## Gotchas

- **IMGUI's built-in font is missing glyphs.** `∞` and `→` rendered as blanks in the HUD, while `·` was fine. Only the real build showed this, so use ASCII or ship a font.
- **Fusion's installer changes settings on editor load.** It sets `PlayerSettings.runInBackground = true` and adds scripting defines for every build target. Both are expected ([SDK in CI](../netcode/sdk-in-ci.md#fusions-defines-and-the-il-weaver)).
- **Keep code that must build without the SDK free of Fusion types.** The networking files are wrapped in `#if FUSION2`. Everything else talks to [`NetContracts.cs`](../../../Assets/Scripts/NetContracts.cs).
