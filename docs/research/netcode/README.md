# Netcode: online two-player co-op with Photon Fusion 2

What we built, how it's shaped, and where the detail lives. Online co-op went live on 2026-10-14 and was played in a real two-player session that day ([test log](../verification/test-log.md)).

**Read this when** you're touching anything networked, adding a feature that two players must see the same way, or choosing what to change for 3–4 players or versus play.

Up: [Research index](../AGENTS.md)

## What exists

- **Joining a game:** host a private game with a four-letter room code like `ABCD`, join by code or by invite link `?room=ABCD`, or quick-match into any open game. Every game is in Photon's Australian region.
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
| Photon **Fusion 2, Shared Mode** | The only Fusion topology available for WebGL; Photon recommends Fusion for new games | [Choosing a stack](choosing-a-stack.md) |
| Each client simulates its own soldier; the master client runs the world | No input lag for your own soldier; one authority for enemies and score; survives the host leaving | [Architecture](architecture.md#authority-model) |
| The SDK lives in a private release, fetched by CI | Photon's license forbids redistributing it | [SDK in CI](sdk-in-ci.md) |
| Network objects built in code, not prefabs | Keeps the project code-only | [Architecture](architecture.md#network-objects-without-prefabs) |
| One region (Australia); a room code is the four-letter session name | Photon sessions exist per region, and the owner chose AU (NET-05) | [Sessions](sessions.md#regions-and-room-codes) |
| Gameplay talks to interfaces, and the Fusion code is its own assembly that only compiles with the SDK | The game builds without the private SDK, and the compiler keeps game code off Fusion | [Architecture](architecture.md#the-boundary-between-game-and-netcode) |

## Documents

- [choosing-a-stack.md](choosing-a-stack.md): Photon's SDKs compared (Realtime 5, Fusion 2, PUN 2, Quantum 3), the 2D frameworks sanity check, the free tier and licensing.
- [sdk-in-ci.md](sdk-in-ci.md): the private vendor release, the read-only token, the checksum, unpacking without Unity, Fusion's defines and weaver in a single CI pass, and upgrading the SDK.
- [architecture.md](architecture.md): authority, replicated state, the RPC table, network objects without prefabs, the per-frame sync, the camera, damage, pickups, restart and host migration.
- [sessions.md](sessions.md): host, join and quick match, room codes, the create-or-join trap, build versions, failure messages, console logs, hidden tabs, and how to test by hand.

Source files:
- [`Assets/Scripts/Core/NetContracts.cs`](../../../Assets/Scripts/Core/NetContracts.cs): the interfaces and event payloads between game and netcode
- [`Assets/Scripts/Net/`](../../../Assets/Scripts/Net/), the `PooketSlug.Net` assembly: `NetSession.cs` (connecting and the session's life), `NetReplication.cs` (the replicated objects and the game's link onto them), `NetPrefabs.cs`, `PlayerNet.cs`, `WorldNet.cs`
- [`Assets/Scripts/Core/NetConfig.cs`](../../../Assets/Scripts/Core/NetConfig.cs): the AppId and region the build bakes in
- [`Assets/Scripts/World/NetSync.cs`](../../../Assets/Scripts/World/NetSync.cs), the world's side of the session: the per-frame pull and push, and the session's events
- [`Assets/Scripts/RemotePlayer.cs`](../../../Assets/Scripts/RemotePlayer.cs)
- [`Assets/Scripts/PlayerVisual.cs`](../../../Assets/Scripts/PlayerVisual.cs)
- [`Assets/Scripts/Core/AvatarPose.cs`](../../../Assets/Scripts/Core/AvatarPose.cs) and [`InviteCode.cs`](../../../Assets/Scripts/Core/InviteCode.cs), with [tests](../verification/edit-mode-tests.md)
