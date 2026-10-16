# Sessions and matchmaking

How players find each other and what happens when things go wrong: hosting, room codes, joining, quick match, build versions, failure messages, console logs, and how to test it by hand. The code is in [`NetSession.cs`](../../../Assets/Scripts/Net/NetSession.cs) (sessions) and [`Game.cs`](../../../Assets/Scripts/Game.cs) (the screens).

**Read this when** you're changing the lobby or matchmaking, a player can't join, or you're about to test co-op.

Up: [Netcode](README.md)

## The player's view

| Screen | Keys | What happens |
|---|---|---|
| Title | **Enter / X** play solo · **H** host · **J** join with a code · **Q** quick match | The online line only appears when the build has the SDK *and* an AppId. Otherwise it reads *Online co-op is off in this build* |
| Join box | Type the code · **Enter** join · **Esc** back | Opens prefilled when the page URL has `?room=ABCD` |
| Connecting | **Esc** cancel | Shows the session's status text. On failure, back to the title with a red notice for 8 s |
| Playing (co-op) | **Esc** twice within 2 s to leave | The bottom line shows the room code, players (n/2) and ping. While alone it shows *Waiting for a partner*, the code and an invite link |
| Mission complete | **Enter** play again · **Esc** leave | Either player's Enter restarts both |

There's no pause in co-op, and no game over: fallen soldiers come back.

## Starting a session

`NetSession.Connect` creates a fresh `NetworkRunner` each time, because runners can't be restarted. It also adds `NetworkSceneManagerDefault` and `NetworkObjectProviderDefault`, as Fusion's own bootstrap does, and calls:

```csharp
runner.StartGame(new StartGameArgs {
    GameMode = GameMode.Shared,
    SessionName = name,                 // null = quick match (random join or create)
    PlayerCount = 2,
    IsVisible = quickMatch,             // private rooms aren't in the random-join pool
    SceneManager = …, ObjectProvider = …,
    CustomPhotonAppSettings = settings, // AppId, AppVersion, FixedRegion (+ WebSocketSecure on WebGL)
});
```

- **No `Scene` is passed.** The game spawns into whatever scene is loaded.
- **On success,** the client spawns its `PlayerNet`. The master client spawns `WorldNet` once.
- **The game starts playing when `INetSession.Ready` is true:** online, `WorldNet` present, and our own `PlayerNet` spawned.

## Regions and room codes

- **Every game is in one Photon region: Australia (`au`),** the owner's choice (NET-05). Hosting, joining and quick match all set `FixedRegion` to `NetConfig.Region`. A build can name another region with `-photonRegion` (`pooket-net.json`); the workflow doesn't pass one.
- **Photon sessions exist per region,** so a fixed region is what lets a code be just a name: everyone looks in the same place.
- **Hosting picks a 4-letter session name** from `ABCDEFGHJKLMNPQRSTUVWXYZ` (no I or O), and that name is the code, for example `ABCD`. [`InviteCode`](../../../Assets/Scripts/Core/InviteCode.cs) makes and parses the codes, and has [tests](../verification/edit-mode-tests.md).
- **Older codes still parse.** Until 2026-10-14 each code carried its region, as in `ABCD-EU`; the suffix is now ignored.
- **Players far from Australia get more lag.** That's the cost of one region.
- **The invite link is the page URL plus `?room=CODE`.** The game reads it from `Application.absoluteURL`, so opening the link prefills the join box.

## The create-or-join trap

- **Shared Mode treats a session name as create-or-join.** A mistyped code doesn't fail: it creates a new, empty room, and you sit in it alone.
- **The fix is a check after joining by code.** If this client came in as the session's master client, nobody was hosting that code. It leaves and shows *Nobody is hosting ABCD*.
- The same check would misfire if the host left in the instant before you joined. That's acceptable.

## Quick match

- `SessionName = null`, `IsVisible = true`: Photon joins a random open visible session or creates one.
- **Everyone quick-matches in the same region,** so any two players can meet.

## Build versions

- **`AppVersion` is set to `Application.version`,** which is `0.2.0+<commit>`. Photon separates matchmaking by AppVersion, so **two different deployed builds never end up in one room** with mismatched network code.
- The flip side: after a redeploy, players on an old tab can't meet players on a fresh one. **Both reload.** It's also why doc-only pushes don't trigger a deploy ([workflow](../build-pipeline/workflow.md#triggers-permissions-and-concurrency)).

## Players and skins

- **`PlayerCount = 2`.** A third player gets *That game is full*.
- **Skins:** a new soldier takes red (skin 0) unless another player already has it. If two players picked red at the same moment, the one with the higher `PlayerId` switches to blue.

## When things fail

`ShutdownReason` is mapped to a short message (`NetSession.Describe`):

| Reason | Message |
|---|---|
| `GameNotFound` | ROOM NOT FOUND |
| `GameIsFull` / `GameClosed` | THAT GAME IS FULL / CLOSED |
| `MaxCcuReached` | PHOTON IS FULL RIGHT NOW, TRY LATER (the free plan's 20 CCU cap) |
| `InvalidRegion` | UNKNOWN PHOTON REGION IN THIS BUILD |
| `InvalidAuthentication`, `CustomAuthenticationFailed` | PHOTON REJECTED THIS BUILD'S APP ID |
| timeouts | CONNECTION TIMED OUT |
| anything else | DISCONNECTED (REASON) |

While connecting (`NetSession.DescribeConnectFailure`), the vague reasons, `Error`, the timeouts and `ConnectionRefused`, read **COULDN'T REACH PHOTON, CHECK YOUR CONNECTION** instead. The named reasons above keep their messages.

- **During play,** `Game` checks `INetSession.Ready` every frame. If the session is gone, it drops to the title with the reason.

## Console logs

Open the browser's developer console. Lines starting with `[PooketSlug/Net]` trace the session:

```text
[PooketSlug/Net] Online in region au: room ABCD, 1/2 players, we are [Player:1] (master client)
[PooketSlug/Net] This client now runs the shared world
[PooketSlug/Net] [Player:2] joined
[PooketSlug/Net] [Player:2] left
[PooketSlug/Net] No game called ABCD in region au; leaving
[PooketSlug/Net] Couldn't go online: <ShutdownReason>
[PooketSlug/Net] Session ended: <ShutdownReason>
[PooketSlug/Net] Disconnected from Photon: <NetDisconnectReason>
```

- **Channels:** `Net` for the session, `Game` and `Audio` for the rest ([`Log.cs`](../../../Assets/Scripts/Core/Log.cs)). Builds before 2026-10-14 printed a plain `[PooketSlug]` prefix.
- **Turning them up or down:** add `?log=net:debug` to the page address, or `?log=all:warn` for quiet. The levels are `debug`, `info` (the default), `warn`, `error` and `off`.
- **`?log=game:debug` also reports the entity pools** each time one's use doubles, from 64: `[PooketSlug/Game] Projectile pool: 128 used, 11 made` ([`EntityPool.cs`](../../../Assets/Scripts/EntityPool.cs)).

Fusion and Photon log with their own prefixes, for example `[Fusion] StartGame Failed: …`, and WebSocket errors come from the browser.

## Hidden tabs

- **Browsers pause `requestAnimationFrame` in hidden tabs,** and Unity WebGL's main loop runs on it.
- **A backgrounded player freezes,** and after a timeout Photon drops them. If that player was the master, the other inherits the world.
- **A frozen host doesn't freeze the game for long** (rooms, 2026-10-16). Every player's `PlayerNet` carries a beat that changes four times a second while their game runs. When the master's beat stops for 2 s, the partner asks Photon for the master client (`NetworkRunner.RequestMasterClientReassign`, [`MasterWatch`](../../../Assets/Scripts/Core/Handover.cs)), and Photon agrees once the master's connection has been idle for `MasterClientRequestReassignThreshold` (2 s, opted into in `NetworkProjectConfig.fusion`). The shared world, a master-client object, moves with it. A host who comes back follows the partner's world. CI's handover round freezes the host's page to check it.
- **Test with two visible windows side by side, or two devices.** Not two tabs in one window.

## Testing by hand

1. Open https://jhiggins-tech.github.io/pooket-slug/?unlock=coop in two windows side by side, or on two devices. Co-op stays hidden until Mission one is beaten solo ([design](../../design/levels.md#what-the-player-sees-and-when)); `?unlock=coop` opens it for that visit without saving.
2. Window A: press **H**, or **CO-OP** then **HOST A GAME**. Note the room code at the top, for example `ABCD`.
3. Window B: press **J**, type the code, then **Enter**. Or open `…/pooket-slug/?unlock=coop&room=ABCD` and press **Enter** or **JOIN**. Without `unlock`, a player who hasn't beaten Mission one is told co-op opens after it, and the mission starts.
4. Check:
   - red P1 and blue P2 soldiers
   - the same enemies on both screens
   - a crate goes to whoever touches it first
   - the screen scrolls for both, and won't leave the trailing soldier behind
   - a death respawns after about 2.5 s
5. In A, press **Esc** twice. B should continue alone with *Waiting for a partner*, and log `This client now runs the shared world`.
6. Quick match: press **Q** in both windows.

The automated version is the [two-browser test](../verification/browser-tests.md#two-browser-co-op-test). It needs a network that allows WebSockets.
