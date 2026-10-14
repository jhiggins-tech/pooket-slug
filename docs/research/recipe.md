# Recipe: reproduce it from scratch

The minimum steps to get a Unity WebGL game building on GitHub Actions and deployed to GitHub Pages, then to add Photon Fusion co-op without committing the SDK. Each step links to the detail.

**Read this when** you're setting this up in another repository.

Up: [Research index](AGENTS.md)

## Part A: GameCI → WebGL → GitHub Pages

1. **Pick a Unity version that has a GameCI WebGL image** (`unityci/editor:ubuntu-<version>-webgl-3`) and put it in `ProjectSettings/ProjectVersion.txt`. → [version and image](build-pipeline/unity-project.md#version-and-editor-image)
2. **Add the repository secrets** `UNITY_LICENSE` (the `.ulf` file's contents), `UNITY_EMAIL` and `UNITY_PASSWORD`. → [GameCI activation](https://game.ci/docs/github/activation)
3. **Write an editor build method** that:
   - ensures a scene exists and is in the build list
   - sets `PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled`
   - reads extra flags from `Environment.GetCommandLineArgs()`
   - builds to the path GameCI passes as `-customBuildPath`
   - exits with `EditorApplication.Exit(0 or 101)`

   → [build script](build-pipeline/unity-project.md#the-build-script)
4. **Write the workflow:**
   1. Free disk space.
   2. Checkout.
   3. Cache `Library`.
   4. `game-ci/unity-builder@v4` with `targetPlatform: WebGL`, `buildMethod`, `customParameters` and `versioning: None`.
   5. Upload the artifact.
   6. `peaceiris/actions-gh-pages@v4` publishing `build/WebGL/WebGL` to `gh-pages`.

   Grant `permissions: contents: write`. Path-ignore `**.md` and `docs/**`. → [workflow](build-pipeline/workflow.md)
5. **Set Settings → Pages → Deploy from a branch → `gh-pages` / `(root)`.** → [pages](build-pipeline/github-pages.md#deploying)
6. **Optionally, type-check locally** against the image's DLLs before the first push. → [type-check](verification/type-check.md)

## Part B: add Photon Fusion 2 co-op (Shared Mode)

1. **In the Photon dashboard,** create an app with SDK **Fusion** and version **Fusion 2**, and copy the AppId. → [choosing a stack](netcode/choosing-a-stack.md#photons-options)
2. **Store the SDK privately:**
   1. Download the Fusion SDK `.unitypackage` while logged in.
   2. Create a **private** repo and attach the package to a release there.
   3. Create a fine-grained token with **Contents: Read-only** on that repo only.
   4. Add the token as `VENDOR_PAT` and the AppId as `PHOTON_APP_ID`.

   → [SDK in CI](netcode/sdk-in-ci.md#how-it-works)
3. **Add a workflow step before the Unity build** that runs `gh release download`, `sha256sum -c` and the unpacker ([`tools/unpack_unitypackage.py`](../../tools/unpack_unitypackage.py)). Git-ignore `Assets/Photon/` except your `NetworkProjectConfig.fusion`, which lists `AssembliesToWeave`. → [SDK in CI](netcode/sdk-in-ci.md)
4. **Add the engine modules Fusion needs** to the manifest (`ai`, `animation`, `imageconversion`, `physics`, `physics2d`, `unitywebrequest`), plus `com.unity.nuget.mono-cecil` at the editor's bundled version. → [modules](build-pipeline/unity-project.md#packages-and-engine-modules)
5. **Pass the AppId through `customParameters`** (`-photonAppId ${{ secrets.PHOTON_APP_ID }}`). Have the build method write it somewhere the game reads at runtime, and set it on `FusionAppSettings` for `StartGame`. → [the AppId](netcode/sdk-in-ci.md#the-appid)
6. **Design authority first:** decide what each client owns and what the master client owns. Keep gameplay behind interfaces, so a build without the SDK still compiles. → [architecture](netcode/architecture.md)
7. **Check the build log** for `IL weaver compiled: True` (or equivalent) and the `FUSION2` define. Then **test with two visible browser windows on a normal network.** → [sessions](netcode/sessions.md#testing-by-hand)
