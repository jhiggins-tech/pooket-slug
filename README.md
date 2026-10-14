# pooket-slug

A spike to test **[GameCI](https://game.ci)** on GitHub Actions. It builds a small Metal Slug–style side-scrolling shooter in **Unity 6.3 LTS** for **WebGL**, then publishes it to **GitHub Pages**.

▶ **Play:** https://jhiggins-tech.github.io/pooket-slug/ · 📝 **Spike write-up:** [LEARNED-LESSONS.md](LEARNED-LESSONS.md)

## What's in the demo

- Run, jump, crouch and shoot forward, up, or down while airborne. There are grenades, one-way bridges to drop through, and coyote-time and buffered jumps.
- Three enemy types: riflemen (crouch under their shots), knife runners, and bomb-dropping drones. A **tank boss** at the end lobs shells and fires low bursts you have to jump.
- Pickups: Heavy Machine Gun crate, bomb crate and medkit. There's also health, score, a persisted hi-score, pause, mute and restart.
- **No imported assets.** All pixel art (ASCII sprites in `ArtData.cs`), level tiles, parallax backgrounds and sound effects are generated from code at startup. The only scene is an empty one created by the build script, and `Bootstrap` spawns the game into it.

Controls: arrows/WASD move · Z/Space jump · X fire · C grenade · Up aim up · Down crouch (or aim down in the air) · Down+Jump drops through bridges · P pause · M mute · R restart. Gamepads work too.

## Pipeline

`.github/workflows/webgl-pages.yml` runs on pushes to `main` and `claude/**`, and on manual dispatch:

1. Frees disk space on the hosted runner. The `unityci/editor` WebGL image is about 7 GB compressed.
2. Downloads the **Photon Fusion SDK** from a release in the private repo `jhiggins-tech/pooket-slug-vendor`, verifies its SHA-256, and unpacks it into `Assets/Photon/` with `tools/unpack_unitypackage.py`. Photon's license forbids redistributing the SDK, so it is never committed here; only our `NetworkProjectConfig.fusion` is.
3. Restores the cached `Library/` folder.
4. Runs **`game-ci/unity-builder@v4`** with `targetPlatform: WebGL` and the custom build method `PooketSlug.EditorTools.WebGLBuild.Build`. The Unity version comes from `ProjectSettings/ProjectVersion.txt`.
5. Uploads the build as a workflow artifact.
6. Pushes the build to the **`gh-pages`** branch with `peaceiris/actions-gh-pages`.

The build script (`Assets/Editor/WebGLBuild.cs`) does the following:

- Creates the scene.
- Uses the custom WebGL template in `Assets/WebGLTemplates/PooketSlug`.
- Stamps the commit SHA into the version shown in-game.
- Disables WebGL compression. GitHub Pages can't serve Unity's pre-compressed files with the right `Content-Encoding` header, so the build ships uncompressed and Pages gzips it on the fly.

### One-time setup

- Repository secrets:
  - `UNITY_LICENSE` (contents of the `.ulf` file), `UNITY_EMAIL`, `UNITY_PASSWORD`. See [GameCI activation](https://game.ci/docs/github/activation).
  - `VENDOR_TOKEN`: a fine-grained token with **Contents: Read-only** on `pooket-slug-vendor`, used to download the Photon SDK.
- **Settings → Pages → Build and deployment → Source: Deploy from a branch → `gh-pages` / `(root)`.**

## Local development

First get the Photon Fusion SDK (`Photon-Fusion-2.1.3-Stable-2390.unitypackage`, from Photon or the `fusion-2.1.3` release of `pooket-slug-vendor`) and either import it in the editor or run `python3 -I tools/unpack_unitypackage.py <package> .`. It lands in `Assets/Photon/`, which git ignores.

Open the folder with Unity **6000.3.26f1**, open any scene (or a new empty one) and press Play. The game bootstraps itself. **Pooket Slug → Build WebGL** writes a build to `Builds/WebGL`.

Project layout:

| Path | What |
| --- | --- |
| `Assets/Scripts/Game.cs` | Bootstrap, camera, state machine (title / playing / game over / victory), IMGUI HUD |
| `Assets/Scripts/World.cs` | Entities, spawning, collisions, explosions, scrolling camera, parallax |
| `Assets/Scripts/Level.cs` | ASCII tile map, tile queries, chunked level rendering |
| `Assets/Scripts/Entity.cs` | Entity/Actor base classes and tile physics (AABB vs grid, one-way bridges) |
| `Assets/Scripts/Player.cs`, `Enemies.cs`, `Projectile.cs` | Gameplay |
| `Assets/Scripts/Art.cs`, `ArtData.cs`, `Sfx.cs` | Procedural sprites, backgrounds and audio |
| `Assets/Scripts/GameInput.cs` | Input that works with either the Input System package or the legacy Input Manager |
| `Assets/Editor/WebGLBuild.cs` | Build entry point for GameCI and the editor menu |
| `Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion` | Our Fusion config (weaver settings); the rest of `Assets/Photon/` is the SDK and is ignored |
| `tools/unpack_unitypackage.py` | Unpacks a `.unitypackage` into the project without Unity (used by CI) |
