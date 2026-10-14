# Co-op architecture

How two clients share one game: who simulates what, what's replicated, which messages exist, how network objects are made without prefabs, and how the game loop syncs. The C# compiler and Fusion's own IL weaver both checked this design ([verification](../verification/fusion-weaver.md)), and a person played it live on 2026-10-14.

**Read this when** you're adding anything both players must see (a weapon, enemy, pickup, vehicle or event), changing the player count, or debugging desync.

Up: [Netcode](README.md)

## Authority model

| What | Simulated by | How others see it |
|---|---|---|
| Your soldier: movement, aim, firing, grenades, health, respawn | **Its owner** | `PlayerNet` properties, smoothed by `RemotePlayer`; shots replayed by RPC |
| Enemies, boss, AI targeting, enemy shots | **The master client** | `WorldNet.Enemies` snapshot → puppets; shots replayed by RPC |
| Spawning (the level's spawn cursor), crates, score, victory | **The master client** | `WorldNet` properties |
| Camera scroll | **The master client** (follows the soldiers) | `WorldNet.CamX`; others follow forward-only |
| Hits on enemies | The shooter's client **reports**, the master **applies** | The master's snapshot shows the result |
| Damage to a soldier | **The victim's owner** applies it | Projectiles and explosions are judged on the victim's client; the master's melee and contact damage is sent as an RPC |
| Crate pickups | The master **arbitrates**: the first claim wins | Grant RPC to everyone |

Why it's split this way:
- **Your own soldier never waits for the network,** so there's no input lag.
- **One authority for the world** means enemies and score never disagree.
- **Damage is judged where the victim is,** so dodging what you see works.
- **The master client is Photon's,** so it survives the host leaving: `WorldNet` is a *master-client object* and moves to the next master client, whose game starts simulating ([host migration](#host-migration)).

Known trade-off: clients trust each other. A modified client could claim hits or crates. That's acceptable for co-op on the free tier, and it's on the roadmap if versus play arrives.

## The boundary between game and netcode

- **Gameplay code never sees a Fusion type.** [`NetContracts.cs`](../../../Assets/Scripts/Core/NetContracts.cs), in the Core assembly, defines what it needs, using only Core types:
  - `INetLink` is used by `World`. It's made of three smaller interfaces: `ISessionView` (authority, skin, round), `ISoldierChannel` (our soldier out, remote soldiers in) and `IWorldChannel` (the shared state and the events about it).
  - `INetEvents` is the other direction: what the session tells the world. `NetSync` implements it, and the session holds it while a co-op world is playing.
  - Each event is a typed payload: `ShotEvent` (a weapon, origin and velocity), `HitReport`, `PickupGrant`, and damage as a plain number.
  - `INetSession` is used by `Game`: the lobby, state, room code, ping.
  - `IAvatarLink` is a remote soldier, and `NetEnemyState` is one snapshot entry.
- **The netcode never sees `World` either:** it talks to `INetEvents`. That keeps the boundary in one place, so the Fusion code can become its own assembly ([refactor plan](../roadmap/refactor-plan.md), step 8).
- **The Fusion implementation lives in [`Assets/Scripts/Net/`](../../../Assets/Scripts/Net/),** in its own assembly, `PooketSlug.Net`. It references Core and the SDK, and only compiles when `FUSION2` is defined (a define constraint). Nothing references it, so the compiler keeps game code from touching Fusion. It registers itself through `NetSessions.Factory`; without the SDK, the factory stays `null` and the title screen offers solo only.
- **Inside it, `NetSession` connects and `NetReplication` shares.** `NetSession` hosts, joins, finds a quick match, reports failures and owns the runner. `NetReplication` holds the `PlayerNet` and `WorldNet` objects, is the game's `INetLink`, and passes the RPCs' events to `INetEvents`. The lobby, room browser and reconnects will grow in `NetSession`.
- **`World` takes an optional `INetLink`.** `null` means solo, and solo behaviour is unchanged.

## Replicated state

Sizes come from the woven assembly, where Fusion's weaver records them in words of 4 bytes.

**`PlayerNet`**: one per player, state authority = its owner, 8 words.

| Property | Meaning |
|---|---|
| `NetPos`, `NetVel` (`Vector2`) | Feet position and velocity |
| `NetPose` (`int`) | `AvatarPose.Pack()` bits: facing, aim up/down, grounded, crouch, running, dead, muzzle flash, blink, hit flash |
| `NetHp` (`int`) | Health |
| `NetSkin` (`int`) | 0 = red P1, 1 = blue P2 |
| `NetRound` (`int`) | `World.Round + 1` once playing; 0 = still in the lobby. Lets clients ignore a soldier that hasn't reached the current round |

**`WorldNet`**: one per session, a master-client object, 152 words.

| Property | Meaning |
|---|---|
| `Round` (`int`) | Bumped on restart; every client rebuilds its `World` when it changes |
| `CamX` (`float`) | Shared scroll position |
| `Score` (`int`), `Victory` (`NetworkBool`) | Team score, mission won |
| `NextSpawn` (`int`) | Spawn cursor into `Level.Spawns`, so followers place crates and props and a new master continues from it |
| `NextNetId` (`int`) | The next id for something spawned mid-game, so a new master doesn't reuse one |
| `Taken` (`long`) | One bit per spawn index for collected crates. The level has 33 spawns; the limit is 64 |
| `Enemies` (`NetworkArray<NetEnemy>`, capacity 24) | `NetEnemy`: `Ref` (the enemy's id + 1, 0 = empty), `Kind` (the `EnemyDef` id), `Flags` (dying, facing, moving, engaged, grounded), `Hp`, `Pos`. 6 words each |

- **An enemy's identity comes from [`NetIds`](../../../Assets/Scripts/Core/NetIds.cs).** A level spawn's id is its index in `Level.Spawns`, which every client knows without a message. Anything spawned mid-game gets the ids after those (`Spawner.SpawnEnemy`), from `NextNetId`, which a new master continues from. The snapshot carries the id, so followers make the enemy on first sight. Pickups are still level spawns only: dropped crates need a replicated table ([refactor plan](../roadmap/refactor-plan.md), step 10).
- **Writes happen from Unity's `Update`, not `FixedUpdateNetwork`.** Shared Mode lets the state authority set networked properties at any time. Fusion's own Shared-Mode sample does the same (`PlayerInteract.Update()` → `[Networked]` writes).

## RPCs

| RPC | On | Sources → targets | Purpose |
|---|---|---|---|
| `RPC_Shot(weapon, origin, vel)` | `PlayerNet` | StateAuthority → Proxies, `InvokeLocal = false` | Replay a soldier's shot elsewhere as a harmless copy (`Cosmetic = true`) |
| `RPC_Hurt(damage)` | `PlayerNet` | All → StateAuthority | The master's melee or contact damage, applied by the owner |
| `RPC_EnemyShot(weapon, origin, vel)` | `WorldNet` | StateAuthority → Proxies, `InvokeLocal = false` | Replay enemy fire; each client damages only its own soldier |
| `RPC_Hit(enemyId, damage)` | `WorldNet` | All → StateAuthority | A follower's bullet or grenade hit an enemy |
| `RPC_Claim(spawnIndex)` | `WorldNet` | All → StateAuthority | "I touched this crate". The first claim sets the `Taken` bit |
| `RPC_Granted(spawnIndex, player)` | `WorldNet` | StateAuthority → All | Everyone shows the pickup; the winner applies its effect |
| `RPC_RequestRestart()` | `WorldNet` | All → StateAuthority | Restart a won mission (ignored unless `Victory`, so a double press restarts once) |

- **Shots carry a weapon id,** a byte from [`Content`](../../../Assets/Scripts/Core/Content.cs). The receiver makes the weapon's projectile and plays its sound. The ids are pinned by [tests](../verification/edit-mode-tests.md), and an id the receiver doesn't know is ignored.

Fusion rules worth knowing (2.1.3):
- **Naming:** RPC method names must start or end with `Rpc`, in any case.
- **Callers:** an `All → StateAuthority` RPC called by the state authority itself runs locally, because `InvokeLocal` defaults to true.
- **Who sent it:** take an optional last parameter `RpcInfo info = default`. For local calls, use `info.IsInvokeLocal ? Runner.LocalPlayer : info.Source`.
- **Networked collections** are get-only: `[Networked, Capacity(N)] NetworkArray<T> X => default;`. Their element structs must implement `INetworkStruct`. Simple networked properties need `{ get; set; }`.
- **RPC parameters** we use: `byte` (enums are cast), `short`, `Vector2` and `PlayerRef`.

## Network objects without prefabs

The project has no prefab assets, so `NetPrefabs.Register()` in [`NetPrefabs.cs`](../../../Assets/Scripts/Net/NetPrefabs.cs) builds each network "prefab" in code the first time a session starts:

1. Create a `GameObject` under an **inactive** root (`[Network Templates]`, `DontDestroyOnLoad`), so the template itself never runs.
2. Add a `NetworkObject` and the behaviour (`PlayerNet` or `WorldNet`), and set the object flags: `DestroyWhenStateAuthorityLeaves` for players, `MasterClientObject` for the world.
3. Bake it with a `NetworkObjectBaker` subclass that reports an execution order. Otherwise the baker warns about inactive objects.
4. Register it in `NetworkProjectConfig.Global.PrefabTable` with a `NetworkPrefabSourceStatic { Object = template, AssetGuid = <fixed GUID>, AssetPath = name }`, which documents itself as able to "point to an asset or to a runtime-created object".
5. Spawn by the returned `NetworkPrefabId`. Every client registers the same templates in the same order, so the IDs line up. Fusion's default object provider instantiates the template (at the scene root, so active) when the object spawns on each peer.

Scene-object registration isn't affected. Fusion's scene manager only collects `NetworkObject`s with a non-zero `SortKey`, and our baker leaves it at 0. We also pass no `Scene` to `StartGame`.

## The per-frame sync

`World.Tick` runs from `Game.Update` at the game's own frame rate. It doesn't use Fusion's tick. The world's systems are in [`Assets/Scripts/World/`](../../../Assets/Scripts/World/), and `NetSync` is the co-op side:

```text
Sync.Pull()       remote soldiers ← RemoteAvatars (add/remove RemotePlayer puppets), local skin
                  followers only: Score, Victory, taken crates, enemy snapshot → create/update/remove puppets
Player.Tick, RemotePlayer.Tick, Spawner.SpawnAhead, Enemies/Projectiles/Pickups/Effects.Tick
Combat.Collide()  our shots vs enemies (authority applies, followers report), enemy shots vs *our* soldier only,
                  crates → Claim RPC in co-op
Scoring.Tick()    popups rise and fade
View.Tick()       the camera
Sync.Push()       our soldier → PlayerNet; master only: CamX, Score, Victory, NextSpawn, NextNetId, Enemies → WorldNet
```

The session delivers its events (shots, hits, pickups, damage) to `NetSync` too.

- **Remote soldiers** (`RemotePlayer`) aim for the replicated position plus `vel × 0.05 s`, with exponential smoothing (`1 − e^(−18·dt)`). They snap if more than 3 units off.
- **Enemy puppets** on followers glide toward the snapshot position (`1 − e^(−15·dt)`, snapping beyond 4 units), take facing, moving and grounded state from the flags, and run their own death animation when they see `Dying`. Only the master's enemies think.
- **A follower's shot that hits a puppet** flashes it locally and sends `RPC_Hit`. The master's next snapshot carries the result.

## Camera

- **Co-op uses a fixed shared width:** `Tuning.Coop.HalfWidth = 10`, i.e. 20 units. That's the 16:9 stage. Solo uses the window's aspect.
- **The master scrolls toward the leading live soldier** (`lead + 2`), but never past the point where the trailing live soldier would leave the left edge (`trail − 0.5 + halfWidth`). Speed is capped at 12 units/s, forward only. A downed soldier doesn't hold the camera back.
- **Followers move toward `CamX`,** forward only, at up to 24 units/s.
- **Each player is kept inside the shared screen.** A narrower local view (an odd fullscreen aspect) follows the local soldier within it.

## Damage, death and respawn

- **Hostile projectiles and explosions only hurt the local soldier.** On a remote soldier they just detonate visually.
- **The master's runner slashes and tank contact go through `Combat.DamageAvatar`:** local soldier → `Hurt`; remote soldier → `RPC_Hurt`, throttled to once per 0.5 s per soldier.
- **No game over in co-op.** A dead soldier respawns after `Tuning.Coop.RespawnDelay` (2.5 s) at the left edge of the shared screen, with full health, at least 5 charges of the special (grenades, for today's soldier) and 2.5 s of invulnerability.

## Restart and rounds

- **Victory:** the master's boss dies → `Victory` is set and replicated → each client shows *Mission complete*. Enter sends `RPC_RequestRestart`.
- **Restart:** the master bumps `Round` and clears the shared state. Each `Game` sees `Link.Round != World.Round` and rebuilds its `World`. `NetRound` stops the old round's soldiers from showing in the new one.

## Host migration

- **When the master client leaves,** its `PlayerNet` despawns (`DestroyWhenStateAuthorityLeaves`), and Photon makes the other client master. `WorldNet` (`MasterClientObject`) moves to the new master.
- **Fusion handles the transfer; the game only checks a flag.** `NetReplication.IsAuthority` (`WorldNet.HasStateAuthority`) flips, and the console logs `[PooketSlug/Net] This client now runs the shared world`.
- **The new authority's puppets become real enemies.** Their AI timers start fresh.
- **Spawning resumes** from `max(own cursor, NextSpawn)`, and mid-game ids from `NextNetId`.
- **Tested on every build:** the [CI co-op test](../build-pipeline/workflow.md#the-two-player-co-op-test-job) makes the master leave and checks that the other client takes over. First confirmed on 2026-10-14 (run 38573051144).
