# pooket-slug

A spike to test **[GameCI](https://game.ci)** on GitHub Actions. It builds a small Metal Slug–style side-scrolling shooter in **Unity 6.3 LTS** for **WebGL**, then publishes it to **GitHub Pages**.

▶ **Play:** https://jhiggins-tech.github.io/pooket-slug/ · 📚 **Research notes and reference guide:** [docs/research](docs/research/AGENTS.md) (start with [lessons learned](docs/research/lessons-learned.md))

## What's in the demo

- Run, jump, crouch and shoot forward, up, or down while airborne. There are grenades, one-way bridges to drop through, and coyote-time and buffered jumps.
- Three enemy types: riflemen (crouch under their shots), knife runners, and bomb-dropping drones. A **tank boss** at the end lobs shells and fires low bursts you have to jump.
- Pickups: Heavy Machine Gun crate, bomb crate and medkit. There's also health, score, a persisted hi-score, pause, mute and restart.
- **Online two-player co-op** with [Photon Fusion 2](https://doc.photonengine.com/fusion/current/fusion-intro) in Shared Mode. Host a game and share its room code or invite link, join with a code, or quick-match. A partner can drop in mid-mission. The screen scrolls for both players, and a fallen soldier comes back after a couple of seconds.
- **No imported assets.** All pixel art (ASCII sprites in `ArtData.cs`), level tiles, parallax backgrounds and sound effects are generated from code at startup. The only scene is an empty one created by the build script, and `Bootstrap` spawns the game into it.

Controls: arrows/WASD move · Z/Space jump · X fire · C grenade · Up aim up · Down crouch (or aim down in the air) · Down+Jump drops through bridges · P pause · M mute · R restart. Gamepads work too.

Co-op, from the title screen: **H** hosts a game and shows its room code (for example `ABCD-EU`, which is the session name plus the Photon region), **J** joins with a code, and **Q** quick-matches. Opening the page with `?room=ABCD-EU` fills in the code. In co-op, press **Esc** twice to leave. Keep the game's tab visible: browsers pause hidden tabs, which freezes that player and eventually drops them. To try it alone, use two side-by-side windows, not two tabs.

### How co-op works

Fusion's Shared Mode is the topology Fusion supports for WebGL. There is no host process: Photon's cloud relays state between the clients.

- **Your own soldier** is simulated by your client and replicated by a `PlayerNet` object you own.
- **The shared world** is simulated by the room's master client: enemies, pickups, score, the scrolling camera and the spawn cursor. It's published through `WorldNet`, a *master-client object*, so if the host leaves, the next master client inherits it and its game becomes the authority. The other clients mirror the snapshot.
- **Hits on enemies** are reported to the authority. **Damage to a soldier** is applied by that soldier's own client. **Crates** go to whoever the authority hears from first.
- Gameplay code only talks to `INetLink` / `INetSession` (`NetContracts.cs`). The Fusion code in `Assets/Scripts/Net/` compiles only when the SDK is present (`#if FUSION2`), so the game still builds as a solo game without it.
- The project has no prefabs, so the two network objects are built in code too: templates are baked with `NetworkObjectBaker` and registered in Fusion's prefab table under fixed GUIDs.

## Pipeline

`.github/workflows/webgl-pages.yml` runs on pushes to `main` and `claude/**`, and on manual dispatch:

1. Frees disk space on the hosted runner. The `unityci/editor` WebGL image is about 7 GB compressed.
2. Downloads the **Photon Fusion SDK** from a release in the private repo `jhiggins-tech/pooket-slug-vendor`, verifies its SHA-256, and unpacks it into `Assets/Photon/` with `tools/unpack_unitypackage.py`. Photon's license forbids redistributing the SDK, so it is never committed here; only our `NetworkProjectConfig.fusion` is.
3. Restores the cached `Library/` folder.
4. Runs **`game-ci/unity-builder@v4`** with `targetPlatform: WebGL` and the custom build method `PooketSlug.EditorTools.WebGLBuild.Build`. The Unity version comes from `ProjectSettings/ProjectVersion.txt`. The Photon AppId is passed as `-photonAppId`, and the build bakes it into `Resources/pooket-net.json`.
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
  - `VENDOR_PAT`: a fine-grained token with **Contents: Read-only** on `pooket-slug-vendor`, used to download the Photon SDK.
  - `PHOTON_APP_ID`: the **Fusion 2** AppId from the [Photon dashboard](https://dashboard.photonengine.com). Without it the build still works, but co-op is off.
- **Settings → Pages → Build and deployment → Source: Deploy from a branch → `gh-pages` / `(root)`.**

## Local development

First get the Photon Fusion SDK (`Photon-Fusion-2.1.3-Stable-2390.unitypackage`, from Photon or the `fusion-2.1.3` release of `pooket-slug-vendor`) and either import it in the editor or run `python3 -I tools/unpack_unitypackage.py <package> .`. It lands in `Assets/Photon/`, which git ignores.

To play co-op from the editor, create `Assets/Resources/pooket-net.json` containing `{"appId": "<your Fusion AppId>", "region": ""}`. Git ignores it. An empty `region` means the best region by ping. Without the SDK, the project still compiles and plays solo.

Open the folder with Unity **6000.3.26f1**, open any scene (or a new empty one) and press Play. The game bootstraps itself. **Pooket Slug → Build WebGL** writes a build to `Builds/WebGL`.

Project layout:

| Path | What |
| --- | --- |
| `Assets/Scripts/Game.cs` | Bootstrap, camera, state machine (title / playing / game over / victory), IMGUI HUD |
| `Assets/Scripts/World.cs` | Entities, spawning, collisions, explosions, scrolling camera, parallax |
| `Assets/Scripts/Level.cs` | ASCII tile map, tile queries, chunked level rendering |
| `Assets/Scripts/Entity.cs` | Entity/Actor base classes and tile physics (AABB vs grid, one-way bridges) |
| `Assets/Scripts/Player.cs`, `Enemies.cs`, `Projectile.cs` | Gameplay |
| `Assets/Scripts/RemotePlayer.cs`, `PlayerVisual.cs` | The partner's soldier (a smoothed puppet) and the soldier rendering both share |
| `Assets/Scripts/NetContracts.cs` | What gameplay needs from an online session (`INetLink`, `INetSession`) |
| `Assets/Scripts/Net/` | Photon Fusion implementation: `NetSession` (runner, rooms, prefabs), `PlayerNet`, `WorldNet`, `NetConfig` |
| `Assets/Scripts/Art.cs`, `ArtData.cs`, `Sfx.cs` | Procedural sprites, backgrounds and audio |
| `Assets/Scripts/GameInput.cs` | Input that works with either the Input System package or the legacy Input Manager |
| `Assets/Editor/WebGLBuild.cs` | Build entry point for GameCI and the editor menu |
| `Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion` | Our Fusion config (weaver settings); the rest of `Assets/Photon/` is the SDK and is ignored |
| `tools/unpack_unitypackage.py` | Unpacks a `.unitypackage` into the project without Unity (used by CI) |
| `DESIGN.md` | The game design: crew, kits, missions, progression, ranks |
| `docs/design/` | The design as four sign-off documents: story and cutscenes, tanks, player mechanics, and maps and progression. Start at [`README.md`](docs/design/README.md) |
| `docs/research/` | Research notes and the reference guide: pipeline, netcode, verification, test log, roadmap. Start at [`AGENTS.md`](docs/research/AGENTS.md) |
