# Netcode: online two-player co-op with Photon Fusion 2

What we built, how it's shaped, and where the detail lives. Online co-op went live on 2026-10-14 and was played in a real two-player session that day.

**Read this when** you're touching anything networked, adding a feature that two players must see the same way, or choosing what to change for 3–4 players or versus play.

Up: [Research index](../README.md)

## What exists

- **Joining a game:** there's one co-op room, MATES: CO-OP joins it, to play with a seat free or to watch once both are taken (missions you've cleared only). `?room=NAME` names another room (CI uses it). Every game is in Photon's Australian region.
- **Two soldiers, red P1 and blue P2,** on one shared, forward-only scrolling screen. The camera can't leave the trailing soldier behind.
- **A shared world:** the same enemies, boss, crates and score for both players. Hits, damage and crate pickups are arbitrated.
- **Players coming and going:** a partner can drop in mid-mission, and a fallen soldier respawns after 2.5 s (there's no game over). If the host leaves, the other player inherits the world. Restarting after victory restarts both players.
- **Solo is unchanged** and still works offline. Builds without the SDK compile solo-only.

## Shape

```text
             Photon Cloud (Fusion 2.1.3, Shared Mode: relays state and RPCs, no host process)
                 ▲                                    ▲
       PlayerNet (owned by A)               PlayerNet (owned by B)
       WorldNet  (owned by the master client: A here; moves to B if A leaves)
                 │                                    │
   ┌─────────────┴──────────────┐       ┌─────────────┴──────────────┐
   │ Client A (master client)   │       │ Client B                   │
   │  simulates: own soldier,   │       │  simulates: own soldier    │
   │  enemies, boss, pickups,   │       │  mirrors: enemies (puppets │
   │  score, camera, spawns     │       │  from WorldNet's snapshot),│
   │  publishes: WorldNet       │       │  partner (RemotePlayer),   │
   │                            │       │  score, camera             │
   └────────────────────────────┘       └────────────────────────────┘
   Game code sees only INetLink / INetSession; Fusion types stay in Assets/Scripts/Net/.
```

## Key decisions

| Decision | Why | Detail |
|---|---|---|
| Photon **Fusion 2, Shared Mode** | The only Fusion topology available for WebGL; Photon recommends Fusion for new games | Choosing a stack |
| Each client simulates its own soldier; the master client runs the world | No input lag for your own soldier; one authority for enemies and score; survives the host leaving | [Architecture](architecture.md#authority-model) |
| The SDK lives in a private release, fetched by CI | Photon's license forbids redistributing it | [SDK in CI](sdk-in-ci.md) |
| Network objects built in code, not prefabs | The project has no prefab assets, so the objects are made at runtime | [Architecture](architecture.md#network-objects-without-prefabs) |
| One region (Australia); one room, MATES, by session name | Photon sessions exist per region, and the owner chose AU (NET-05) | [Sessions](sessions.md#one-room) |
| Gameplay talks to interfaces, and the Fusion code is its own assembly that only compiles with the SDK | The game builds without the private SDK, and the compiler keeps game code off Fusion | [Architecture](architecture.md#the-boundary-between-game-and-netcode) |

## Documents

- [sdk-in-ci.md](sdk-in-ci.md): the private vendor release, the read-only token, the checksum, unpacking without Unity, Fusion's defines and weaver in a single CI pass, and upgrading the SDK.
- [architecture.md](architecture.md): authority, replicated state, the RPC table, network objects without prefabs, the per-frame sync, the camera, damage, pickups, restart and host migration.
- [sessions.md](sessions.md): the one room, joining and watching it, build versions, failure messages, console logs, hidden tabs, and how to test by hand.

Source files:
- [`Assets/Scripts/Core/Platform/NetContracts.cs`](../../../Assets/Scripts/Core/Platform/NetContracts.cs): the interfaces and event payloads between game and netcode
- [`Assets/Scripts/Net/`](../../../Assets/Scripts/Net/), the `PooketSlug.Net` assembly: `NetSession.cs` (connecting and the session's life), `NetReplication.cs` (the replicated objects and the game's link onto them), `NetPrefabs.cs`, `PlayerNet.cs`, `WorldNet.cs`
- [`Assets/Scripts/Core/Platform/NetConfig.cs`](../../../Assets/Scripts/Core/Platform/NetConfig.cs): the AppId and region the build bakes in
- [`Assets/Scripts/World/NetSync.cs`](../../../Assets/Scripts/World/NetSync.cs), the world's side of the session: the per-frame pull and push, and the session's events
- [`Assets/Scripts/Crew/Shared/RemotePlayer.cs`](../../../Assets/Scripts/Crew/Shared/RemotePlayer.cs)
- [`Assets/Scripts/Crew/Shared/PlayerVisual.cs`](../../../Assets/Scripts/Crew/Shared/PlayerVisual.cs)
- [`Assets/Scripts/Core/Crew/AvatarPose.cs`](../../../Assets/Scripts/Core/Crew/AvatarPose.cs) and [`CoopRoom.cs`](../../../Assets/Scripts/Core/Lobby/CoopRoom.cs), with [tests](../verification/edit-mode-tests.md)
