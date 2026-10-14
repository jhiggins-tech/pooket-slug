# Choosing a networking stack

Why Photon Fusion 2 in Shared Mode, what else was considered, and the sanity check on whether any existing 2D framework should have replaced our hand-written gameplay. The research was done 2026-10-14; product versions are as of that date.

**Read this when** you're questioning the stack, considering Quantum, PUN or Netcode for GameObjects, adopting a framework or asset, or checking the free tier and license limits.

Up: [Netcode](README.md)

## Photon's options

| SDK | What it is | Verdict for this game |
|---|---|---|
| **Fusion 2.1** (2.1.3, Sep 2026, supports Unity 6.3) | High level: networked properties, RPCs, objects owned by one player, interpolation | **Chosen.** WebGL is officially supported in **Shared Mode**, with an official WebGL sample updated Sep 2026 |
| **Realtime 5** (5.1.20, Aug 2026) | Low level: connect, matchmake, rooms, raise events; you sync whatever you like | The first recommendation, because it fits a plain-C# simulation. Dropped because its download page doesn't list WebGL, our only target |
| **PUN 2** | The older all-in-one package | No. Photon says it's in "maintenance / LTS mode… New projects should consider Fusion or Quantum" |
| **Quantum 3** | A deterministic engine where every client runs the same simulation | Overkill. It would mean rewriting the simulation in Quantum's ECS with fixed-point maths |

Why the recommendation changed from Realtime to Fusion:
- **WebGL:** Fusion's WebGL support is official, and GitHub Pages is the only target.
- **Our code fits Fusion Shared:** each client moves its own soldier locally, which is exactly Shared Mode's model.
- **Photon's direction:** Fusion is Photon's recommended path for new games, and Fusion apps qualify for the free 100 CCU commercial plan.

Fusion facts that shaped the design:
- **Only Shared Mode runs on WebGL.** Photon says the Client/Server (Host) modes are "disabled" for WebGL by default and "not recommended". In Shared Mode there's no host process: each client has state authority over its own objects, and a *master client* exists for shared objects.
- **Get Fusion 2.1.3 Stable from Photon's download page,** which needs a login. Not the 2.0.x line, and not the Asset Store listing, which was older (2.0.9) and under the stricter Asset Store license.
- **A Fusion AppId is its own app type.** Create it in the dashboard with SDK "Fusion", version "Fusion 2". A Realtime app's AppId doesn't work with Fusion.
- **Fusion's IL weaver rewrites networked code at compile time,** and it works in headless CI builds ([SDK in CI](sdk-in-ci.md)).

## Are we reinventing the wheel? The 2D frameworks check

We checked each hand-written system against Unity's built-in 2D tools, established frameworks and Photon's samples. A background agent did the research, and the claims the verdicts lean on were spot-checked against their sources. **Bottom line: keep almost everything hand-written.** At the game's size when we checked (about 2,500 lines of code-only C#, before co-op), shipping to WebGL from a public repo, our own systems fit better than any framework.

| System | Alternative considered | Verdict |
|---|---|---|
| Tile physics (AABB vs grid, one-way bridges) | Tilemap + colliders + `PlatformEffector2D`, Rigidbody2D | **Keep.** In Shared Mode the owner moves its own avatar, so no physics sync is needed. Fusion's `NetworkRigidbody2D` is "Only for Client Host or Dedicated Server modes". Unity 6.3's new Box2D v3 API needs compute shaders, which WebGL2 lacks |
| Character controller (coyote time, jump buffer, drop-through) | Tarodev Ultimate 2D Controller (MIT); Photon's KCCs | **Keep.** Ours already matches. Tarodev's free version lacks one-way platforms, and Photon's KCCs are 3D capsule controllers |
| Level data (ASCII map) | Tilemap + LDtk (LDtkToUnity) or Tiled (SuperTiled2Unity), both MIT | **Keep; consider LDtk** when levels grow |
| Art (ASCII sprites, palette swaps) | Aseprite Importer, 2D Animation, Sprite Atlas | **Keep** until an artist joins |
| Camera and parallax | Cinemachine + Confiner2D; Pixel Perfect Camera | **Keep.** Cinemachine has no one-way scrolling, and in co-op the scroll lock is networked state |
| Enemy AI (small state machines) | Corgi AIBrain, behaviour-tree assets | **Keep.** It's small, and it runs on the master client |
| Projectiles and effects (a GameObject each) | `ObjectPool<T>`; pooling in Fusion's `INetworkObjectProvider` | **Adopt pooling** ([roadmap](../roadmap/README.md)) |
| HUD (IMGUI) | uGUI or UI Toolkit | **Adopt with the next UI work.** Unity says IMGUI "is not generally intended to be used for normal in-game user interfaces" |
| Input (polled snapshot) | Input System actions | **Consider later**, for rebinding and touch |
| Sound (synthesised clips) | Feel ($50) | **Keep** |
| Bootstrap (no prefabs) | Fusion objects built at runtime | **Keep.** Fusion doesn't force prefabs ([how](architecture.md#network-objects-without-prefabs)) |

What was ruled out, and why:
- **Corgi Engine:**
  - 10.0 needs Unity 6.5, so a 6.3 project gets 9.4.
  - It's a paid Asset Store asset, so it can't go in a public repo.
  - It's local multiplayer only, with no netcode.
  - Adopting it would replace nearly all our code.
- **Other controllers:**
  - Platformer PRO 2 is deprecated.
  - Character Controller Pro has no netcode.
  - Prime31's CharacterController2D is CC BY-NC-SA, not MIT, and was last updated in 2019.
  - Other MIT controllers are years stale.
- **Networked starting points:**
  - **Quantum Platform Shooter 2D** is the closest genre match, but it's arena PvP and would mean a Quantum rewrite.
  - **Fusion Razor Madness** uses Host/Client, the wrong topology for WebGL.
  - **Fusion Forecast Physics 2D** is Shared Mode but physics mini-games, so it's useful only as a Rigidbody2D reference.
- **Unity's official 2D samples** target 2022.3 or 2020.3, and none is a networked shooter.

## Free tier and licensing

- **Free 20 CCU plan:**
  - That's 10 two-player sessions at once. The cap is hard: the next client is rejected with `MaxCcuReached`.
  - 3 GB of traffic per CCU per month.
  - **Development and non-commercial use only.** A public demo is fine; a released product isn't.
- **Free 100 CCU plan:** allows a commercial launch, but it's for **Fusion and Quantum only**, with one app per customer. Realtime apps don't qualify.
- **The AppId ships inside the public WebGL build.** That's unavoidable for a client app, and the hard CCU cap limits abuse.
- **Photon's license** (our reading, not legal advice):
  - You may not "distribute, transfer or grant sublicenses to a Software", and the SDK is confidential.
  - Copies are allowed "as necessary for the use… with an Application".
  - So the SDK and Photon sample code stay out of this public repo. The built game is fine. How CI gets the SDK: [SDK in CI](sdk-in-ci.md).
- **Unity Asset Store EULA:**
  - Assets may be distributed only "as incorporated and embedded" in a product.
  - **No Asset Store package can be committed to a public repo, free ones included.**
  - Extension Assets are licensed per seat, though build-farm servers don't need their own seat.
  - Any paid asset would go through the same private vendor repo as the Photon SDK.
- **Safe to commit publicly:**
  - Unity registry packages referenced from the manifest
  - MIT code (Tarodev, LDtkToUnity, SuperTiled2Unity)
  - Unity Companion License samples

## Sources

- **Photon:**
  - Fusion 2 [SDK download & requirements](https://doc.photonengine.com/fusion/v2/getting-started/sdk-download) · [WebGL sample](https://doc.photonengine.com/fusion/v2/technical-samples/fusion-webgl) · [Shared Mode intro](https://doc.photonengine.com/fusion/2-shared/fusion-shared-intro) · [What's new in 2.1](https://doc.photonengine.com/fusion/v2/getting-started/release-notes/whats-new-2-1) · [NetworkObjectProvider](https://doc.photonengine.com/fusion/v2/manual/advanced/network-object-provider) · [IL weaving](https://doc.photonengine.com/fusion/v2/manual/advanced/network-object-weaving) · [AppId instructions](https://doc.photonengine.com/fusion/v2/getting-started/appid-instructions) · [Physics addon](https://doc.photonengine.com/fusion/v2/addons/physics/overview)
  - Realtime 5 [SDK download](https://doc.photonengine.com/realtime/v5/getting-started/sdk-download) · [intro](https://doc.photonengine.com/realtime/v5/realtime-intro) · [release notes](https://doc.photonengine.com/realtime/v5/getting-started/release-notes)
  - [PUN 2 intro](https://doc.photonengine.com/pun/current/getting-started/pun-intro) · [Quantum Platform Shooter 2D](https://doc.photonengine.com/quantum/current/game-samples/platform-shooter-2d/overview) · [Fusion Razor Madness](https://doc.photonengine.com/fusion/v2/game-samples/fusion-razor-madness)
  - [Pricing & CCU plans](https://doc.photonengine.com/photon/current/pricing) · [License terms](https://www.photonengine.com/terms/licenseterms)
- **Unity:**
  - [LowLevelPhysics2D requirements](https://docs.unity3d.com/6000.3/Documentation/Manual/2d-physics-api/2d-physics-api-introduction.html) · [WebGL graphics](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-graphics.html) · [`ObjectPool<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1.html) · [UI system comparison](https://docs.unity3d.com/6000.3/Documentation/Manual/UI-system-compare.html) · [Render pipeline strategy 2026](https://unity.com/topics/render-pipelines-strategy-for-2026)
  - [Asset Store EULA](https://unity.com/legal/as-terms) · [EULA FAQ](https://assetstore.unity.com/browse/eula-faq)
- **Third party:** [Corgi Engine releases](https://corgi-engine.moremountains.com/corgi-engine-releases) · [Prime31 CharacterController2D](https://github.com/prime31/CharacterController2D) · [Tarodev Ultimate 2D Controller](https://github.com/Matthew-J-Spencer/Ultimate-2D-Controller) · [LDtkToUnity](https://github.com/Cammin/LDtkToUnity)

Photon's documentation site is behind a JavaScript bot challenge. We read it with headless Chromium ([tool](../verification/tools/README.md)).
