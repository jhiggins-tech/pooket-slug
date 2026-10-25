# The net domain

## What this domain is
The co-op: how two phones share a world (Photon Fusion 2, Shared Mode), the one room they meet in, and the sign-in and
saves that outlive a session. Netcode is the part of the game where two phones must agree without being told to.

## The files
The map (`.claude/hierarchy/OWNERSHIP.txt`) is the authority; this is its list.

- `Assets/Scripts/Net/BossNet.cs`
- `Assets/Scripts/Net/NetPrefabs.cs`
- `Assets/Scripts/Net/NetReplication.cs`
- `Assets/Scripts/Net/NetSession.cs`
- `Assets/Scripts/Net/PlayerNet.cs`
- `Assets/Scripts/Net/WorldNet.cs`
- `Assets/Scripts/Save/` (everything under it: `LocalProfileStore.cs`, `OptionsStore.cs`, `CloudAccount.cs`, `SiteFile.cs`)
- `Assets/Scripts/World/NetSync.cs`
- `Assets/Scripts/Core/Platform/NetContracts.cs`
- `Assets/Scripts/Core/Platform/NetIds.cs`
- `Assets/Scripts/Core/Platform/NetConfig.cs`
- `Assets/Scripts/Core/Platform/Rooms.cs`
- `Assets/Scripts/Core/Platform/Handover.cs`
- `Assets/Scripts/Core/Platform/DropIn.cs`
- `Assets/Scripts/Core/Platform/PartnerBeacon.cs`
- `Assets/Scripts/Core/Platform/WireMeter.cs`
- `Assets/Tests/EditMode/Platform/DropInTests.cs`
- `Assets/Tests/EditMode/Platform/MasterWatchTests.cs`
- `Assets/Tests/EditMode/Platform/NetConfigTests.cs` (new 25 Oct, not yet in OWNERSHIP.txt: handed to the architect)
- `Assets/Tests/EditMode/Platform/NetIdsTests.cs`
- `Assets/Tests/EditMode/Platform/PartnerBeaconTests.cs`
- `Assets/Tests/EditMode/Platform/RoomBrowserTests.cs`
- `Assets/Tests/EditMode/Platform/WireMeterTests.cs`
- `Assets/Tests/Gameplay/NetSyncTests.cs`
- `Assets/Plugins/WebGL/PooketAuth.jslib`
- `Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion`

Close to this domain but not its files: `Core/Lobby/CoopRoom.cs` and `Core/Lobby/Profile.cs` (shell), `Core/Tabks/CoopTabks.cs`
(gameplay), `Crew/Shared/RemotePlayer.cs` and `Core/Crew/AvatarPose.cs` (gameplay), `Net/PooketSlug.Net.asmdef` (architect),
`tools/coop.mjs` and `.github/workflows/webgl-pages.yml` (the pipeline). A change here that needs one of them is a handover.

## The rules this domain keeps
- Game code reaches Photon only through the interfaces in `Core/Platform/NetContracts.cs` (`INetSession`, `INetLink`,
  `INetEvents`). Fusion types stay in `Assets/Scripts/Net/`, an assembly that compiles only with `FUSION2` defined, so the
  game still builds solo without the SDK. (docs/research/netcode/architecture.md)
- Shared Mode: every client simulates its own soldier; the room's master client runs the shared world (enemies, boss,
  pickups, score, scroll) and publishes it in `WorldNet`; the others puppet it and report their hits. Never scale enemies
  or bosses for co-op. (docs/research/netcode/README.md)
- Content ids and `KitMove` numbers ride the network and never change or get reused. Two phones only match on an
  identical build: `settings.AppVersion = Application.version` in `NetSession.cs`, so each preview is its own co-op world.
  There is no separate wire or rules number; the build is the version.
- A level spawn's network id is its index in `Level.Spawns`; things made mid-game take the next ids from a counter the
  authority keeps, and a new authority carries on from it (`NetIds`, `NetIdsTests`). Ids fit a 16-bit field, so `MaxId` is
  `short.MaxValue - 1`.
- Only the mover's screen does damage. A partner's shot or kit move is replayed as a harmless copy (`KitEvent`,
  `KitCopy`); only authority-simulated goons hurt, and followers report hits to the authority. (docs/research/netcode/architecture.md)
- There is one co-op room, MATES (`CoopRoom`), always there; `?room=NAME` names another (CI uses it so rounds never meet
  real players). With a free seat you play; with both taken you watch, but only a mission you have cleared (no spoilers),
  else you are told the seats are taken. A room holds two players and four watchers. (`RoomBrowser.ActionFor`, `RoomBrowserTests`;
  docs/design/levels.md, Online)
- Every game is in one Photon region, Australia (`au`), the owner's choice (NET-05): `NetConfig.DefaultRegion`, set with
  `FixedRegion`. A build may name another with `-photonRegion`; the workflow does not.
- The shared world follows its master. When the master's beat goes quiet for 2 s, `MasterWatch` asks Photon to make this
  client the master; Photon grants it after its own threshold, set in `NetworkProjectConfig.fusion` (`Handover.cs`, `MasterWatchTests`).
- Core's network files name no Unity scene object or resource (dca-core-no-unity, 25 Oct). `NetConfig` in Core is the
  baked file's shape (`NetConfig.Data`: `Configured`, `Region`) and the region rule (`NetConfig.Region`: trimmed, lower
  case, blank is `au`; `NetConfigTests`); `NetSession.Config` in `Net/` loads `Resources/pooket-net.json` once.
  `NetSessions.Factory` is a `Func<INetSession>`: `NetSession.Create` gives null without an AppId, else makes its own
  host object ("Net Session", kept across loads), so `Game` only calls `NetSessions.Factory?.Invoke()`.
- Fusion's own config (`NetworkProjectConfig.fusion`) is committed and wins over the one the SDK ships.
- Public repository: never commit Photon SDK code (CI unpacks it from the private vendor repository), the Photon app id
  (a secret, baked into `Resources/pooket-net.json`, git-ignored) or any key. Say nothing about the SDK's internals beyond
  what docs/research/netcode already says. (docs/research/netcode/sdk-in-ci.md)
- Mission one is solo only; beating it opens co-op (`LaunchFlags` unlock, `Profile`). Co-op never starts there.
- Sign-in is optional and lives on the page: `pooket-auth.js` owns the button, the session and the database calls, the
  game hears it through `SendMessage` to the `PooketAuth` object (`CloudAccount`). `CloudProfileStore` saves locally and,
  while signed in, to the cloud; two copies merge by taking the better of each field, never adding clears
  (`Profile.Merge`, `IProfileStore`). The public identifiers are repository variables, not secrets.
  (docs/research/accounts/README.md, `database-rules.md`)
- Options stay on the device (`OptionsStore`): they are not progress, so they are never synced.
- Every storage call is guarded: a failed read or write must leave the game playable (`LocalProfileStore`, `OptionsStore`).

## The vocabulary
- Seats: P1 is the phone running the shared world, P2 is the other (`NetReplication.SeatOf`). Red and blue are the players' colours (`Palette`).
- Replicated objects: `PlayerNet` (one per soldier, owned by its player), `WorldNet` (owned by the master), `BossNet`.
  Snapshots are capped by `NetLimits` (24 enemies, 16 pickups).
- Session properties, short keys in `RoomBrowser`: `m` mission, `n` soldiers, `w` watching, `g` under way, `h` host (the master's soldier).
  `OpenKey` (`o`) is left over from the public / invite-only switch: nothing sets or reads it. Removing it is a code change, so it needs a card.
- Wire kinds for the meter: `WireKind` (Shot, EnemyShot, Burst, Hit, Claim, Grant, Hurt, Kit). The meter is in the game (every phone logs it each
  30 s), no longer a spike. Burst is never counted outside `WireMeterTests`: kcaj's pills go as `KitMove.Pills`, and `BurstEvent` is the spike's
  shape, kept for its tests. A crewmate's move that is not a
  shot is a `KitMove`/`KitEvent`, and a soldier's pose is one int (`AvatarPose`), so state stays a fixed size whatever is on screen.
- The partner beacon (arrow, low-health and away warnings) and drop-in (own scores beside the shared one) are in the game; their spikes are gone.
- Words players read: "tabk", never tank; the status lines in `NetSession.cs` are player text, so keep to that.

## The invariants
- Gameplay randomness only through a seeded `Rng`; cosmetic effects use their own. Do not read the clock for a rule that both phones must agree on:
  timed hazards run from `World.Elapsed`, so no message is needed (CLAUDE.md, Add a hazard).
- Adding or changing a message means the structs in `Net/` and `NetContracts.cs`, and a kit move gets a new `KitMove` number, never a reused one.
- Followers get the authority's world through the snapshot only; the world talks to the network through `INetLink`, never a Fusion type.
- When the authority changes, nothing may restart: the new one adopts the shared id counter (`NetIds.CatchUp`) and the replicated state.
- Nothing in `Core/Platform` may name Unity scene objects or Fusion. It is plain C# with tests; a rule found in `Net/` or `NetSync` that does not need Fusion moves into Core with tests the next time it changes.
- Photon's free plan has 20 connections in all; the room's limits (`RoomBrowser.MaxWatchers`, `Tuning.Coop.MaxPlayers`) keep within it.
- Changing the netcode runs the two-browser co-op rounds in CI; they do not run on a push that does not touch it unless a commit says "CI: co-op".
  "The netcode" is the plan job's paths: `Assets/Scripts/Net/`, `World/NetSync.cs`, `Core/Platform/NetContracts.cs` and `tools/coop.mjs`,
  so a comment-only change there runs them too. The rest of `Core/Platform` and `Save/` don't.

## How it is checked
- `tools/check.sh` compiles every assembly as Unity would, runs Fusion's weaver and the EditMode tests. Locally there is no Fusion SDK
  in some sessions, so the Net assembly may only be fully checked by CI: push the branch and read the run.
- Rules: `Assets/Tests/EditMode/Platform/` (`DropInTests`, `MasterWatchTests`, `NetIdsTests`, `PartnerBeaconTests`,
  `RoomBrowserTests`, `WireMeterTests`, `NetConfigTests`), and `Assets/Tests/EditMode/Tabks/CoopTabksTests.cs` for the tabk rule it shares with gameplay.
- The world's side: `Assets/Tests/Gameplay/NetSyncTests.cs` (a real `World` against a stand-in session, CI only).
- End to end: `tools/coop.mjs`, run by CI in modes `code`, `boss`, `scene`, `dropin` (allowed to fail the build), `handover`, `watch`
  and `debrief`. The `code` round also checks that a tabk is given way when both load the same one.
- By hand: two visible windows, never two tabs (browsers pause a hidden tab and stop the connection). A preview's Try it link
  is its own room. github.io is blocked from cloud sessions, so serve the `gh-pages` branch locally to smoke-test.

## Known unfinished
- Two phones arriving within a second of each other can both take the last seat: the listing is a second behind (docs/research/netcode/sessions.md). Open.
- A watcher does not take a seat that frees up without leaving and joining again. Open.
- A watcher sees the touch pad, which does nothing for them. Open (checked 25 Oct: `HudView.Pad` shows it whatever `info.Watching` says; `HudView` is the shell's file, so the fix is a handover to it).
- The `dropin` round is allowed to fail in CI (`optional: true` in the workflow). Drop-in itself is in the game; the round's flakiness is not understood.
- Cloud progression beyond the profile: XP and rank already go to the cloud (`Profile.xp`, `Profile.rank`, mapped both ways by
  `pooket-auth.js`'s `toProfile` / `fromProfile`, checked 25 Oct). `tokens` and the `tally` are in the database rules
  (docs/research/accounts/database-rules.md) but the page doesn't map them; "xp-cloud" was parked by the owner on 16 Oct until he queues it.
- Photon outages have happened (16 Oct, every CI connect refused, cleared by itself). If it recurs it is Photon's side, not ours.
- Players far from Australia get more lag; no second region is planned (NET-05).
- Max two players. Three or four, or versus, is not designed (docs/research/netcode/README.md).
- Work needs its own Roadmap card, queued by the owner (https://claude.ai/artifact/1AWnQDGtUmiKMnjPgcbru9). None is open for this domain today.
- Stale docs after dca-core-no-unity (25 Oct, unowned, handed to the architect): docs/research/verification/edit-mode-tests.md
  says Core reads the build's net config and that `NetConfig` isn't tested; docs/research/netcode/sdk-in-ci.md says
  `NetConfig.cs` loads the file (`NetSession.cs` does now).
- docs/research/netcode is claimed by no domain in OWNERSHIP.txt, so this domain can't fix it. Stale there on 25 Oct (handed to the architect):
  architecture.md says `ISessionView` carries a skin (it carries the seat; `NetSkin` was retired 21 Oct), and that `NetReplication` holds
  `PlayerNet` and `WorldNet` (and `BossNet`); README.md's list of `Net/` files leaves out `BossNet.cs`.
