# Learned lessons: GameCI → Unity WebGL → GitHub Pages spike

**Question:** can GameCI on GitHub Actions build a Unity WebGL game and publish it to GitHub Pages with little effort?

**Answer: yes.** A Unity 6.3 LTS project went from an empty repository to a playable, publicly hosted build in one session. Every workflow run that finished passed, the first one included. Each push to the branch now redeploys in about 5–7 minutes.

Live build: https://jhiggins-tech.github.io/pooket-slug/

## The numbers

| | Run #1 (cold) | Run #3 (Library cache restored) |
| --- | --- | --- |
| Whole job | 6 min 41 s | 5 min 33 s |
| `game-ci/unity-builder@v4` step (image pull + activation + build) | 330 s | 259 s |
| Free disk space step | 58 s | 60 s |
| Upload artifact + deploy to `gh-pages` | ~5 s | ~6 s |

- **Library cache:** 83 MB compressed, saving about 70 s per build.
- **WebGL output:** about 23.8 MB uncompressed. That's the `.wasm` (18.9 MB), `.data` (4.4 MB), framework JS (0.4 MB) and loader plus `index.html` (~32 KB).
- **Over the wire:** Pages gzips everything on the fly, so a visitor downloads about **7.2 MB**. That's the `.wasm` at 5.8 MB, the `.data` at 1.3 MB and the framework at 0.09 MB.
- **GitHub Pages:**
  - After switching the Pages source to `gh-pages`, the first publish took a couple of minutes, and GitHub's own "pages build and deployment" jobs took about 20 s.
  - The site returned 404 until that finished.
  - Pages serves `.wasm` as `application/wasm` and loaded the build in about 1.8 s in headless Chromium.
- **Toolchain:**
  - Editor image: `unityci/editor:ubuntu-6000.3.26f1-webgl-3`. Its editor layer alone is 7.1 GB compressed.
  - Unity reported engine revision `6000.3.26f1 (de8167e7440c)` at runtime.
- **Runs:** #2 was cancelled on purpose. The workflow's concurrency group cancels an in-flight run when a newer commit is pushed.

## What worked well

- **GameCI v4 with a Personal license worked first time.** It needed only the three secrets: `UNITY_LICENSE` (contents of the `.ulf` file), `UNITY_EMAIL` and `UNITY_PASSWORD`. There was no license-activation debugging at all.
- **A custom `buildMethod` gave full control, with little code.** GameCI passes `-customBuildPath` (and `-buildTarget`), and anything extra goes through `customParameters`. We used that to pass `-buildSha ${{ github.sha }}`. One editor script (`Assets/Editor/WebGLBuild.cs`) then does four things:
  - creates the scene
  - sets PlayerSettings
  - selects the WebGL template
  - stamps the commit SHA into `bundleVersion`, so the deployed page shows exactly which commit it is.
- **Writing the project in code avoided hand-authoring Unity YAML.** The committed project is just:
  - `ProjectSettings/ProjectVersion.txt`
  - `Packages/manifest.json`
  - the C# scripts plus `.meta` files

  Unity generated every other ProjectSettings file on first open in CI. A `[RuntimeInitializeOnLoadMethod]` bootstrap spawns the game into an empty scene. Sprites, tiles, backgrounds and sounds are all generated at startup, so there was no asset import pipeline to set up.
- **Deploying through a `gh-pages` branch decoupled deploys from `main`.** We used `peaceiris/actions-gh-pages`. The official `actions/deploy-pages` route goes through the `github-pages` environment, which by default only accepts deployments from the default branch. Pushing to a `gh-pages` branch let a feature branch deploy with no environment changes. The only manual step was Settings → Pages → Source → `gh-pages`.
- **Turning WebGL compression off is the simplest choice for Pages.** Pages can't set the `Content-Encoding` header that Unity's pre-compressed Gzip/Brotli files need. We set `compressionFormat = Disabled`, Pages gzips responses itself (`content-encoding: gzip`, 23.8 MB → 7.2 MB), and nothing had to be configured on the server.
- **The custom WebGL template's syntax is unchanged in Unity 6.3.** It still uses `{{{ VAR }}}` and `#if` / `#endif`. CSS lines that start with `#` (like `#stage { … }`) are not mistaken for directives; Unity's own templates do the same. We set `codeUrl` unconditionally instead of depending on `#if USE_WASM`.

## Gotchas and decisions worth remembering

- **Your Unity version must exist as a GameCI image.** Check that `unityci/editor:ubuntu-<version>-webgl-3` is on Docker Hub before you pin `ProjectVersion.txt`; don't assume a brand-new patch release already has one. At the time of the spike, the 6000.0 and 6000.3 LTS lines and the 6000.4–6000.6 tech-stream lines all had WebGL images.
- **`unity-builder` v4 or v6:**
  - v6 (August 2026) is a thin wrapper around `game-ci/cli`, on Node 24.
  - v4.8.x is the long-standing implementation, still getting backports, on Node 20.
  - We used v4. The cost is a "Node.js 20 is deprecated … forced to run on Node.js 24" warning, which is harmless today.
  - We moved `actions/cache` (v5) and `actions/upload-artifact` (v6) to their Node 24 majors.
  - Revisit v6 once it has a few point releases.
- **`versioning` defaults to `Semantic`.** That needs full git history (`fetch-depth: 0`). We set `versioning: None` and passed the SHA ourselves.
- **"Active Input Handling" is unknown when Unity generates ProjectSettings.** Rather than guess, `GameInput.cs` supports both backends through `#if ENABLE_INPUT_SYSTEM` / `ENABLE_LEGACY_INPUT_MANAGER`.
  - Pin `com.unity.inputsystem` to the version bundled inside the editor image. For 6000.3.26f1 that's 1.20.0, found under `Editor/Data/Resources/PackageManager/Editor/`. That way package resolution needs no registry fetch.
  - The Input System's "enable the new backends and restart?" dialog is skipped in batch mode (`!Application.isBatchMode` in `InputSystemEditorInitializer.cs`), so it can't hang CI.
- **IMGUI's built-in font is missing glyphs.** `∞` and `→` rendered as blanks in the HUD; `·` was fine. Only looking at the real build caught this, so use ASCII or ship a font.
- **Free disk space before the build.** The editor image is large, so we run `jlumbroso/free-disk-space` first, which costs about 60 s. We didn't test whether today's `ubuntu-latest` would cope without it, so treat that as an optimisation to try.
- **The concurrency group is global.** `group: webgl-pages` with `cancel-in-progress` means a push to any branch cancels any in-flight build. That's fine for one branch; change it to per-branch groups before several people push.
- **Every successful build overwrites `gh-pages`, whichever branch it came from.** That's the right model for a spike. A team would want `main` at the root and previews in subfolders.

## Verifying without a Unity license (reusable trick)

There was no Unity install or license outside CI. Two techniques still let us check the work before pushing, which is why the first CI run went green.

1. **Type-check C# against the GameCI image's own Unity DLLs.**
   - Stream the editor layer from Docker Hub with `curl … | tar -xz --wildcards …`, keeping only these folders:
     - `Editor/Data/Managed/UnityEngine/*.dll`
     - `Editor/Data/NetStandard`
     - `Editor/Data/DotNetSdkRoslyn`
     - `Editor/Data/NetCoreRuntime`
   - That's about 250 MB on disk, though the whole 7 GB layer still downloads.
   - Compile with Unity's own `csc.dll` and `-langversion:9.0`, which is the C# version Unity uses.
   - Compile three times: with `ENABLE_LEGACY_INPUT_MANAGER`, with `ENABLE_INPUT_SYSTEM`, and with both. The Input System package's runtime sources compile too, apart from its `InputForUI` plugin, which relies on Unity-internal APIs.
   - Result: zero errors and zero warnings before the first push, and no compile failures in CI.
2. **Smoke-test the real build in a headless browser.**
   - Serve the `gh-pages` branch, or the live URL, and drive it with Playwright + Chromium (SwiftShader software WebGL 2).
   - This proved the template renders, the engine boots, input reaches the game, and nothing errors in the console.
   - The FPS counter reads about 20–29 in software rendering. That says nothing about real-GPU performance.

For the procedural art, a tiny Python script rendered the ASCII sprite arrays to a PNG sheet. That made it possible to iterate on art without the editor.

## If we take this further

- **Deploys:** publish `main` to the Pages root and branches to `/<branch>/` previews. Use `destination_dir` + `keep_files`, or switch to `actions/deploy-pages` with environment rules.
- **Tests:** add `game-ci/unity-test-runner` for EditMode/PlayMode tests ahead of the build job.
- **Supply chain:** pin third-party actions to commit SHAs.
- **Size vs. simplicity:** probably not worth changing. Pages' on-the-fly gzip already brings the download to 7.2 MB. Brotli plus `decompressionFallback` would likely save only another 15–20%, and the browser would have to decompress it in JavaScript.
- **Content pipeline:**
  - commit real scenes and prefabs once designers are involved
  - set up sprite import settings, or an Aseprite import route, for real art
  - get a real font for the HUD (TextMesh Pro, or an atlas)
- **Gameplay gaps found during the spike:**
  - no touch controls, so it's keyboard or gamepad only
  - the automated play test only covers the first few seconds
  - the later level and the tank boss still need a human playthrough
- **Performance:** measure on real devices and GPUs; the headless numbers aren't meaningful.
- **More platforms:** the same workflow becomes a `targetPlatform` matrix for desktop and Android builds.

## Minimal recipe

1. Pick a Unity version with a `unityci/editor:ubuntu-<ver>-webgl-3` image and put it in `ProjectSettings/ProjectVersion.txt`.
2. Add the `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` repository secrets.
3. Add an editor build method that:
   - ensures a scene exists
   - sets `PlayerSettings.WebGL.compressionFormat = Disabled`
   - builds to the path given by `-customBuildPath`
   - exits with the build result (`EditorApplication.Exit`).
4. Write the workflow:
   1. free disk space
   2. checkout
   3. cache `Library`
   4. `game-ci/unity-builder@v4` with `targetPlatform: WebGL`, `buildMethod` and `versioning: None`
   5. upload the artifact
   6. `peaceiris/actions-gh-pages` publishing `build/WebGL/WebGL`.
5. Set Settings → Pages → Source → Deploy from a branch → `gh-pages` / `(root)`.
