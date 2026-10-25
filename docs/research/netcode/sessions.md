# Sessions and matchmaking

How players find each other and what happens when things go wrong: the one co-op room, joining and watching it, build versions, failure messages, console logs, and how to test it by hand. The code is in [`NetSession.cs`](../../../Assets/Scripts/Net/NetSession.cs) (sessions) and [`Game.cs`](../../../Assets/Scripts/Game/Game.cs) (the screens).

**Read this when** you're changing the lobby or matchmaking, a player can't join, or you're about to test co-op.

Up: [Netcode](README.md)

## The player's view

| Screen | Keys | What happens |
|---|---|---|
| Title | **Enter / X** START · SETTINGS | The title has START and SETTINGS only. Co-op is reached from the den |
| Den | **H**: join the co-op room, where everyone meets in the one room, MATES | Only when the build has the SDK *and* an AppId, and co-op is open. Otherwise it reads *Online co-op is off in this build* |
| Den, in the room | **H** or tap the TV: share or copy the invite link | The TV shows the room's name (MATES) and how many are watching |
| Connecting | **Esc** cancel | Shows the session's status text. On failure, back to the title with a red notice for 8 s |
| Playing (co-op) | **Esc** twice within 2 s to leave | The bottom line shows the room's name, players (n/2) and ping. While alone it shows *Waiting for a partner* and an invite link |
| Mission complete | **Enter** play again · **Esc** leave | Either player's Enter restarts both |

There's no pause in co-op. Each player has their own lives and continues (`Tuning.Lives`, `LifeBank`): a fallen soldier with a life left comes back after about 2.5 s.

## Starting a session

`NetSession.Connect` creates a fresh `NetworkRunner` each time, because runners can't be restarted. It also adds `NetworkSceneManagerDefault` and `NetworkObjectProviderDefault`, as Fusion's own bootstrap does, and calls:

```csharp
runner.StartGame(new StartGameArgs {
    GameMode = GameMode.Shared,
    SessionName = name,                 // MATES, or the link's ?room=
    PlayerCount = RoomBrowser.MaxPeople, // 2 players + 4 watching
    IsVisible = false,                  // nobody browses: everyone joins it by name
    SessionProperties = …,              // the room's listing: every key given at creation, so the lobby shows them
    SceneManager = …, ObjectProvider = …,
    CustomPhotonAppSettings = settings, // AppId, AppVersion, FixedRegion (+ WebSocketSecure on WebGL)
});
```

- **No `Scene` is passed.** The game spawns into whatever scene is loaded.
- **On success,** the client spawns its `PlayerNet`. The master client spawns `WorldNet` once.
- **The game starts playing when `INetSession.Ready` is true:** online, `WorldNet` present, and our own `PlayerNet` spawned.

## One room

- **There's one co-op room, `MATES`, always there** (Jack, 18 Oct: few enough players, all mates). H in the den, or the den's spare controller, joins it; the first phone in opens it. [`CoopRoom`](../../../Assets/Scripts/Core/Lobby/CoopRoom.cs) names it, with [tests](../verification/edit-mode-tests.md). Hosting, room codes, the join box, quick match, the room browser and the public / invite-only switch are gone (they were built 14–17 Oct).
- **Each build has its own room.** `AppVersion` is `Application.version`, so a preview's players meet each other, not the main line's.
- **`?room=NAME` names another room** (upper-cased; an old `-EU` region suffix is ignored). The CI co-op rounds use it so rounds running at once, and real players, never meet. The den's invite is just the page link, or the page plus `?room=NAME` from another room.
- **Every game is in one Photon region: Australia (`au`),** the owner's choice (NET-05), set with `FixedRegion` from `NetConfig.Region`. A build can name another region with `-photonRegion` (`pooket-net.json`); the workflow doesn't pass one. Players far from Australia get more lag.

## The room's rules and watching

- **The room's listing** is in its session properties: its mission (`m`), players with a soldier (`n`), people watching (`w`), whether a mission is under way (`g`) and the host's soldier (`h`). The master client rewrites them when they change, checked once a second. A phone joining reads them and [`RoomBrowser.ActionFor`](../../../Assets/Scripts/Core/Platform/Rooms.cs) decides: a free seat plays; with both seats taken it watches if it has cleared the room's mission (no spoilers), or leaves with BOTH SEATS ARE TAKEN; with four already watching, THE ROOM IS FULL. Photon's own "game is full" (a seventh phone) and "game closed" read THE ROOM IS FULL and THE ROOM IS CLOSED too (`RoomWords`, `RoomBrowserTests`); there is no "room not found", since the first phone in opens the room.
- **Watching** joins the room with no `PlayerNet`. The watcher's world has a soldier who's out for good (`World.Spectate`), its camera follows the room's scroll, and the HUD says WATCHING ROOM MATES. Watchers are the room's peers without a soldier, so the den's TV can count them. A watcher made master client (the host left) hands it to a player, or leaves when nobody's playing.
- **A room holds six:** two players and four watchers (Photon's free plan allows 20 connections in all).
- **Known gaps:** two phones arriving within a second of each other could both take the last seat, as the listing is a second behind; a watcher doesn't take a seat that frees up without leaving and joining again.

## Build versions and hidden tabs

- **Build versions:** `AppVersion` is the build's version (`Application.version`, the commit stamped by the build script), and Photon only matches clients with the same one. Two different builds never meet, so every preview is its own co-op world. It is also why a redeploy during a session splits it, and why [docs-only pushes don't build](../build-pipeline/workflow.md#triggers-permissions-and-concurrency).
- **Hidden tabs:** browsers pause a hidden tab (Unity WebGL runs on `requestAnimationFrame`), which freezes that player and stops servicing the connection. A paused master stops running the shared world. [`MasterWatch`](../../../Assets/Scripts/Core/Platform/Handover.cs) in Core notices when the master's beat goes quiet (2 s) and asks Photon to make this client the master, which Photon grants once its own idle threshold has passed. Test with two visible windows, not two tabs; the CI `handover` round covers this.

## Testing by hand

1. Open https://jhiggins-tech.github.io/pooket-slug/?unlock=coop in two windows side by side, or on two devices. Co-op stays hidden until Mission one is beaten solo ([design](../../design/levels.md#what-the-player-sees-and-when)); `?unlock=coop` opens it for that visit without saving.
2. Window A: start the game to reach the den and press **H** there. It opens the MATES room.
3. Window B: the same. Without `unlock`, a player who hasn't beaten Mission one is told co-op opens after it. To keep out of anyone else's way, add `&room=TEST` to both windows' links.
4. Check:
   - two soldiers, each in their own colours, with red and blue tags and hearts for the seats
   - the same enemies on both screens
   - a crate goes to whoever touches it first
   - the screen scrolls for both, and won't leave the trailing soldier behind
   - a fall with a life left respawns after about 2.5 s
5. In A, press **Esc** twice. B should continue alone with *Waiting for a partner*, and log `This client now runs the shared world`.
6. A third window, with both seats taken, watches.

The automated version is the [two-browser test](../verification/browser-tests.md#two-browser-co-op-test). It needs a network that allows WebSockets.
