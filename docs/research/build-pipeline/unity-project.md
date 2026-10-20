# The Unity project and build script

How the Unity side is set up: the version and editor image, a project that's mostly code, the packages it uses, input handling, and what [`WebGLBuild.cs`](../../../Assets/Editor/WebGLBuild.cs) does.

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

## What's committed

The committed Unity project is small, and almost all of it is code:

| Path | What it is |
|---|---|
| `ProjectSettings/ProjectVersion.txt` | The only committed ProjectSettings file. Unity generates the rest with defaults on every CI run |
| `Packages/manifest.json` | Packages and engine modules (see below) |
| `Assets/Scripts/**.cs`, `Assets/Editor/WebGLBuild.cs` | All gameplay, art, audio and UI, plus the build entry point, each with its `.meta` |
| `Assets/Scripts/Core/` | The pure logic, in its own assembly, `PooketSlug.Core` (an `.asmdef` with `autoReferenced`, so the rest of the game sees it) |
| `Assets/Tests/EditMode/` | NUnit tests for Core, in an editor-only test assembly ([EditMode tests](../verification/edit-mode-tests.md)) |
| `Assets/WebGLTemplates/PooketSlug/index.html`, `pooket-auth.js` | The page around the canvas ([GitHub Pages](github-pages.md#the-webgl-template)), and the sign-in script it loads (the button, the session and the database calls: [accounts](../accounts/README.md)) |
| `Assets/Plugins/WebGL/*.jslib` | The bridges from C# to the page: `PooketAuth` (sign-in and cloud saves), `PooketDemo` (recording the kit demos, [workflow](workflow.md#6b-the-kit-screens-demos)) and `PooketInvite` (sharing the co-op invite link) |
| `Assets/Resources/` | Painted art as data: `Plates/<mission id>/` (the parallax backdrops), `Title/` (the logo) and `Den/`. CI adds the vendor repository's announcer, narrator and music WAVs here (`.bytes`, made into clips in code), and the scenes ([workflow](workflow.md)) |
| `Assets/link.xml` | Keeps `Assembly-CSharp`, `PooketSlug.Core` and `PooketSlug.Net` whole under managed code stripping. Everything is created from code at runtime, so no scene or prefab references keep types alive |
| `Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion` | Fusion's config: only `AssembliesToWeave`, which lists the netcode's assembly ([SDK in CI](../netcode/sdk-in-ci.md)) |

Everything else is generated:
- **The scene:** `Assets/Scenes/Main.unity` is created empty by the build script and git-ignored.
- **The game:** a `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` bootstrap in [`Game.cs`](../../../Assets/Scripts/Game/Game.cs) spawns it into that scene.
- **Content:** sprites (ASCII art in `ArtData.cs`), tiles and sound effects are built at startup. The painted plates and the voice and music WAVs above are loaded from `Resources` as raw bytes and turned into textures and clips in code, so Unity's importers are not involved.
- **Network objects:** built in code too ([architecture](../netcode/architecture.md#network-objects-without-prefabs)).

Why it's worth it:
- Nobody hand-writes Unity YAML.
- Diffs are reviewable.
- The project can be type-checked without Unity ([verification](../verification/README.md)).

The cost:
- Painted art and recorded audio go in as raw bytes, which the game decodes itself.
- A few Unity defaults stay unknown until CI generates them. Active Input Handling is one of them, handled [below](#input).

## Packages and engine modules

`com.unity.modules.*` packages decide which `UnityEngine.<X>Module.dll` assemblies scripts can see. **A module that's missing from the manifest is missing at compile time.** We found out which modules the Photon SDK needs by compiling it against only the enabled ones ([type-check](../verification/type-check.md#emulating-disabled-modules)):

| Package | Needed by |
|---|---|
| `com.unity.inputsystem` 1.20.0 | The input sources in `Assets/Scripts/Input/` (keyboard, gamepad and touch). Pinned to the version **bundled in the editor image** (`Editor/Data/Resources/PackageManager/Editor/`), so resolving packages needs no registry fetch |
| `com.unity.ugui` 2.0.0 | The HUD and menus, built in code ([`Assets/Scripts/Ui/`](../../../Assets/Scripts/Ui/)). It's a core package built into the editor, so it needs no registry fetch. It also turns on the Input System's UI support, whose input module the menus' event system uses. It costs about 3.7 MB of download, mostly UI Toolkit, which its event system reaches because the Input System brings in UI Toolkit's module (numbers) |
| `com.unity.modules.audio`, `imgui`, `jsonserialize` | The game: procedural audio, uGUI's text input (it reads IMGUI's key events), and `JsonUtility` for `pooket-net.json` and the profile |
| `com.unity.modules.physics`, `physics2d` | Fusion.Unity (`RunnerSimulatePhysics`, `Rigidbody2D` helpers). The game itself uses its own tile physics |
| `com.unity.modules.ai` | Fusion.Unity and its editor (NavMesh helpers) |
| `com.unity.modules.animation` | Fusion's editor scripts (`Animator` tooling) |
| `com.unity.modules.unitywebrequest` | Photon Realtime's `PhotonPing.cs` and Fusion's editor |
| `com.unity.modules.imageconversion` | Fusion's editor (`Texture2D.LoadImage`) |
| `com.unity.modules.video` | The ⓘ kit screen's demo clips (`VideoPlayer` into a render texture, [`DemoClip.cs`](../../../Assets/Scripts/Ui/DemoClip.cs)), added 2026-10-16. On WebGL it plays them through the browser's own video element |
| `com.unity.test-framework` 1.6.0 | The EditMode tests. It's built into the editor (a core package), so it needs no registry fetch, and it brings `com.unity.ext.nunit` |
| `com.unity.nuget.mono-cecil` 1.11.6 | Fusion's IL weaver. This is the version bundled with the editor. Listing it also stops Fusion's installer from calling `Client.Add("com.unity.nuget.mono-cecil@1.10.2")` mid-build ([SDK in CI](../netcode/sdk-in-ci.md#fusions-defines-and-the-il-weaver)) |

## Input

- **Active Input Handling is unknown.** It isn't set until Unity generates ProjectSettings, so the [input sources](../../../Assets/Scripts/Input/InputSources.cs) support both backends: `#if ENABLE_INPUT_SYSTEM` (keyboard, gamepad and touch) and the legacy Input Manager (keyboard and touch).
- **Each source adds to one frame.** [`GameInput`](../../../Assets/Scripts/Input/GameInput.cs) polls the keyboard, a gamepad and the touch screen once per frame into a `SoldierInput` (in Core) and a `MenuInput`, so they can be mixed. The world hands `SoldierInput` to the local soldier, so gameplay never reads a device.
- **The touch arcade pad** follows the [design](../../design/player.md#controls). [`ArcadePad`](../../../Assets/Scripts/Core/Input/ArcadePad.cs), in Core and [tested](../verification/edit-mode-tests.md), turns fingers into soldier input; `HudView` draws it. It reads touches itself rather than using the Input System's on-screen controls, which need uGUI's event system and give neither a stick that follows the thumb nor sliding between buttons. It shows once the screen has been touched and hides when a key or gamepad button is pressed. The menus are uGUI buttons, so taps work them directly.
- **The page gives the canvas every touch:** `touch-action: none`, with no text selection or long-press menu. On a phone held sideways, the header and footer hide and the game fits the screen; held upright, the page asks to turn it.
- **The Input System's "enable the new backends and restart?" dialog can't hang CI.** It's skipped in batch mode: `!Application.isBatchMode` in `InputSystemEditorInitializer.cs`.

## The build script

[`Assets/Editor/WebGLBuild.cs`](../../../Assets/Editor/WebGLBuild.cs) is GameCI's `buildMethod`. It's also on the menu at **Pooket Slug → Build WebGL**, which writes to `Builds/WebGL`. In order, it:

1. **Reads its arguments** with `Environment.GetCommandLineArgs()`: `-customBuildPath` (from GameCI), then `-buildSha`, `-photonAppId`, `-photonRegion`, `-googleClientId`, `-firebaseApiKey` and `-firebaseDbUrl` (from `customParameters`; the workflow passes no region). A flag whose value is missing or starts with `-` counts as absent.
2. **Writes the Photon config** (`WriteNetConfig`) to `Assets/Resources/pooket-net.json` (`{"appId": …, "region": …}`, git-ignored), then runs `AssetDatabase.Refresh()`.
   - Without `-photonAppId`, it logs that co-op is off and leaves any existing local file alone.
3. **Ensures a scene** at `Assets/Scenes/Main.unity` and sets it as the only build scene.
4. **Configures the player:**
   - `EditorSettings.serializationMode = ForceText`, which Fusion requires.
   - Company and product names.
   - `bundleVersion` = `0.2.0+<short sha>`, or `0.2.0-local`. This is the version shown in-game, and it's also Photon's `AppVersion` ([sessions](../netcode/sessions.md#build-versions-and-hidden-tabs)).
   - A 960×540 default canvas, `runInBackground`, no splash screen.
   - **WebGL:** compression `Disabled`, `decompressionFallback` off, `nameFilesAsHashes` off, `dataCaching` on, template `PROJECT:PooketSlug`.
   - **Managed stripping is Unity's default, `Minimal`.** `Low` was tried on 2026-10-14 and saved 24 KB of a 42 MB wasm, because what uGUI added can't be stripped.
5. **Runs `AssetDatabase.SaveAssets()`,** then **logs one `[PooketSlug]` diagnostics line:** SDK present, weaver compiled, co-op code compiled, WebGL defines.
6. **Builds** with `BuildPipeline.BuildPlayer`. When it succeeds, it **writes `pooket-config.js`** beside the page (`window.POOKET_CONFIG`: the Google client ID, Firebase API key and database URL; `{}`, so sign-in is off, unless all three are given). It then prints a result summary and exits `0` or `101`.

## Gotchas

- **Unity's built-in font is missing glyphs.** `∞` and `→` rendered as blanks in the old IMGUI HUD, while `·` was fine. The uGUI HUD uses the same built-in font (`LegacyRuntime.ttf`). Only the real build shows this, so use ASCII or ship a font.
- **Keys belong to the game, not to uGUI.** The event system's navigation is off (`sendNavigationEvents = false`), so Enter never presses a button and its shortcut at once. Buttons answer clicks and taps; their keys, shown under them, go through `GameInput`.
- **Fusion's installer changes settings on editor load.** It sets `PlayerSettings.runInBackground = true` and adds scripting defines for every build target. Both are expected ([SDK in CI](../netcode/sdk-in-ci.md#fusions-defines-and-the-il-weaver)).
- **Keep code that must build without the SDK free of Fusion types.** The networking files are their own assembly, `PooketSlug.Net`, with the define constraint `FUSION2`. Everything else talks to [`NetContracts.cs`](../../../Assets/Scripts/Core/Platform/NetContracts.cs).
