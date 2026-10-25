# The shell domain

## What this domain is
The shell around the game: the `Game` state machine and the flows it hands frames to, the screens a player sees outside
a mission (title, settings, pause, game over, victory, the mission screen, the kit screen, the debrief), the HUD, the
input that drives them, and what the player keeps (profile, options, XP and rank, the co-op room's name).

## The files
The map (`.claude/hierarchy/OWNERSHIP.txt`) is the authority. Today it lists:
- `Assets/Scripts/Game/Game.cs`, `Assets/Scripts/Game/BoardFlow.cs`, `Assets/Scripts/Game/KitFlow.cs`,
  `Assets/Scripts/Game/DebriefFlow.cs`, `Assets/Scripts/Game/DemoFlow.cs`, `Assets/Scripts/Game/SandboxFlow.cs`
- `Assets/Scripts/Ui/Ui.cs`, `Assets/Scripts/Ui/MenuView.cs`, `Assets/Scripts/Ui/HudView.cs`,
  `Assets/Scripts/Ui/TitleView.cs`, `Assets/Scripts/Ui/TitleLogoView.cs`, `Assets/Scripts/Ui/DebriefView.cs`,
  `Assets/Scripts/Ui/DemoClip.cs`, `Assets/Scripts/Ui/Invite.cs`
- `Assets/Scripts/Input/`
- `Assets/Scripts/Core/Title/`
- `Assets/Scripts/Core/Lobby/Profile.cs`, `Assets/Scripts/Core/Lobby/Options.cs`, `Assets/Scripts/Core/Lobby/Ranks.cs`,
  `Assets/Scripts/Core/Lobby/Debrief.cs`, `Assets/Scripts/Core/Lobby/CoopRoom.cs`
- `Assets/Tests/EditMode/Lobby/CoopRoomTests.cs`, `Assets/Tests/EditMode/Lobby/DebriefTests.cs`,
  `Assets/Tests/EditMode/Lobby/OptionsTests.cs`, `Assets/Tests/EditMode/Lobby/ProfileTests.cs`
- `Assets/Plugins/WebGL/PooketInvite.jslib`, `Assets/Plugins/WebGL/PooketDemo.jslib`

Not this domain's, though they sit beside its files: the cutscene flows (`SceneFlow`, `BossIntroFlow`), the den's
(`DenFlow`, `BayFlow`, `RoomsFlow`, `RoomFlow`) and the audio's (`MusicFlow`); every file under `Ui/Den/` and
`Ui/Cutscene/`; `Core/Missions/StartFlow.cs` and `MissionBoard.cs` (gameplay); `Core/Input/` and its tests
(gameplay); `Assets/Tests/EditMode/Title/` (sprites); `Assets/Plugins/WebGL/PooketAuth.jslib` (net).

## The rules this domain keeps
- **Core decides, views draw.** A rule found in `Game.cs`, a flow or a view moves into Core with a test the next time
  that code changes; a screen file that does several jobs is split by part the next time it changes. No pass just for
  that. (`CLAUDE.md`, Working rules, "Leave it better"). `Game.cs` is 910 lines and `HudView.cs` 669: they are the
  first candidates.
- **`Game` owns the state and one `World`; each part of the game has a flow** (`Game/*Flow.cs`) that `Game` hands the
  frame to. A new screen is a flow plus a view, not more branches in `Game.cs`.
- **The title is a splash**: the logo, START and SETTINGS, nothing else. Missions, the crew and co-op are the den's.
  (`TitleView.cs` summary, `docs/design/art.md` "The title logo", `docs/design/player.md`.)
- **START goes where `StartFlow` says**: the opening and Mission one until Mission one is beaten, then the den; every
  debrief goes to the den. The shell calls `StartFlow`, it does not decide it (gameplay's, `StartFlow.cs`).
- **Mission one is solo.** Beating it sets the `coop` reveal (`Game.cs`, `Profile.MarkSeen`), which opens co-op and the
  den's crew. (`docs/design/player.md`, `docs/design/levels.md`.)
- **Never let a rank name leak.** Players are told of Bronze, Silver, Gold, Platinum, Diamond, Master, Grandmaster and
  Champion only. The other 25 appear only on a player who holds one, in `DebriefView`, never in a menu, a tooltip or
  the site. `Ranks.cs` is the one file that holds the full ladder; do not copy its names into any other text a player
  reads. Code, tests, docs and commit messages may name them. (`CLAUDE.md`, Hidden ranks.)
- **Rank ids and the ladder order never change**: saves go by the order (`Ranks.cs` comment). A new rank is appended
  by the owner's say, with a row in `Ranks.All`.
- **Saves must read forward.** `Options` is one number (`Options.Bits`): a new setting takes the next free bits and
  reads as its default in an old save. Never move a shift (`OptionsTests.cs`). `Profile` is serialised as it is: add
  fields, never rename or reuse them (`ProfileTests.cs`). `Reveals` ids (`coop`, `tank`) are saved by name.
- **Reveals play once.** A thing shown on first meeting is recorded with `Profile.MarkSeen` and saved at once.
- **Touch first.** Every screen works by tap in landscape on a phone; keyboard and gamepad also work
  (`InputSources.cs`: one `SoldierInput` a frame from whichever sources are present, so they can be mixed). A screen
  holds a press for its `Take*` in a `Latch` (gameplay's `Core/Input/Latch.cs`), never a raw per-frame flag.
- **HUD units**: a canvas is laid out `Tuning.Hud.Height` tall and as wide as the screen; use the `Ui` helpers
  (`Cover`, `Tap`, `Stretch`, `Rect`, `Label`, `Picture`) and `Tuning.Hud.TapGuard` after a screen appears
  (`Ui.cs`, `Debrief.cs`).
- **Words players read**: "tabk", never tank; "Trollogram", never hologram; kie is said "k-eye". Identifiers keep
  their names (`CLAUDE.md`). The kit demo clips (`demos/<id>.mp4`) are recorded by CI from the game itself.
- **Launch flags** (`?test=`, `?unlock=`, `?demo=`, `?sandbox=`, `?room=`, `?mission=`) are read once at start-up
  into `LaunchFlags` (gameplay's, tested). A flag that changes a screen is read by `Game`, not by the view.
- **Co-op room**: one always-there Photon session, `CoopRoom.Mates`; only identical builds meet, so each preview has
  its own. `?room=NAME` names another (the co-op tests use it). (`CoopRoomTests.cs`.)
- **The debrief shows what that phone's player did** (their kills, lives, XP); the score is the shared one. In co-op,
  CONTINUE goes back to the room's den. (`DebriefFlow.cs` summary, `docs/design/cutscenes/mission-one.md`.)
- **Browsers only share or copy inside a tap's own event.** `Invite` arms on finger-down and the page acts on
  finger-up (`Invite.cs`, `PooketInvite.jslib`).

## The vocabulary
- `GameState`: Title, Settings, Lobby, Den, Cutscene, Playing, GameOver, Victory, Demo. Do not rename them.
- `MenuScreen`: None, Title, Settings, Connecting, Paused, GameOver, Victory, Kit, Missions. `MenuAction` is what a
  button asks for. `MenuView.Info` is what `Game` gathers each frame for the menus; `HudView` takes the same kind.
- The flows: `BoardFlow` (mission screen), `KitFlow` (the ⓘ screen), `DebriefFlow`, `DemoFlow` (`?demo=`),
  `SandboxFlow` (`?sandbox=`). The "mission screen" and the "kit screen" are what players and docs say.
- The title logo is ten letters and the tabk (`TitleLogo`), moved by `TitleMotion` as pure functions of seconds since
  the entrance began: the tabk drives in, the letters spring, it hovers over a fixed shadow, and a gold glint sweeps.
  Parts come from `Resources/Title/layout.txt`, made by `tools/title_logo.py`.
- `Volume` (High, Mid, Low, Off) for effects, voices and music; `Options.Crew` and `Options.Tabk` are who and which
  tabk the player last loaded.
- Debrief constants live in `Progression` (`Debrief.cs`): `ClearXp`, `FreedXp`, `KillXp`, `Rung`, `FirstClearClimb`.
  They are placeholders to tune after playtesting. A first clear of Mission one is always worth a whole rung.

## The invariants
- The shell reaches co-op only through `INetSession` (the HUD reads it) and the room through `CoopRoom`; the netcode
  is net's. Co-op starts together play the mission opening on both phones (`LaunchFlags.Testing` skips it).
- `Game` owns the profile and its store; flows save through it. A view never saves.
- A sandbox or a spike run never writes the profile (`SandboxFlow.cs`, `Spike.cs`).
- A demo or a scripted sandbox take hides the build line, frame rate and start banner so the clip is clean
  (`HudView` info flag).
- Core folders know nothing of Unity's scene or the network: `Core/Title`, `Core/Lobby/*` here stay plain C#.
- The title's backdrop stays behind Settings. The logo's entrance plays once a visit and a tap skips it.
- The build is the CI's: there is no Unity licence in a cloud session, so a screen is judged from the preview link.

## How it is checked
`tools/check.sh` (type-check as WebGL, Fusion's weaver, EditMode tests, the scene check, the docs link check); use
`tools/check.sh compile` for a type change. The shell's own tests are `CoopRoomTests`, `DebriefTests`, `OptionsTests`
and `ProfileTests` in `Assets/Tests/EditMode/Lobby/`. Input logic is tested by gameplay's `Assets/Tests/EditMode/Input/`
(`ArcadePadTests`, `LatchTests`, `SoldierInputTests`); the logo's motion by `Assets/Tests/EditMode/Title/TitleMotionTests.cs`
(sprites). **No test covers `Game.cs`, the menus, the HUD, the title or the kit/mission screens**; they are checked by
the CI touch run (`tools/touch.mjs`), the build, and the preview link. To smoke-test a deploy without github.io, serve
the `gh-pages` branch locally and use `?test=debrief` and Enter through to the mission screen. Co-op changes also run
`tools/coop.mjs`; kit demos run `tools/demos.mjs`.

## Known unfinished
- The debrief's rank and XP numbers are placeholders; tuning is waiting on playtesting (`Debrief.cs` summary).
- `Game.cs` still holds rules that belong in Core and is far over a screen's size; no split is planned yet. Each
  change to it should move one rule out with a test (see the rules above).
- No tests for the HUD, menus or the title view. Core logic reached through them is tested, the layout is not.
- The den painting is not at the soldier's scale, so `DenMapTests` has an `[Ignore]` with its reason; that is the den
  domain's, and the reason text was to be updated the next time that file is touched.
- The title-logo animation and its sounds are built (`TitleMotion`, `TitleView.MusicDue`); nothing open there.
- The mission screen opens only from the den (`DenFlow` calls `Board.Show(fromDen: true)`); `BoardFlow`'s other path
  (PLAY going on to the den) is left from the title's old MISSIONS button and is unused. Remove it the next time
  `BoardFlow` changes.
- Hard never shows on the mission screen: `BoardFlow` asks `MissionBoard.Cards(profile, coop: false)` and
  `MissionBoard.HardOpen` needs co-op, and the co-op room has no mission screen of its own. Whether that is meant is
  an owner question (gameplay owns `MissionBoard`); the comments now say what the code does.

## Stale-docs pass (25 Oct)
Comments in the shell's files were checked against the code. Fixed: the title's old MISSIONS, CREW and CO-OP buttons
(`BoardFlow`, `MenuView`, `Options`, `GameInput.Host`), sign-in "to come" (`Profile`), hard's rule (`Setting`), the
partner beacon called a spike (`Ui`, `HudView`), a `partner` spike that is gone (`Game.cs`: `Spike.cs` has only `craters`), the bay pointer to `docs/design/lobby.md`, The tabk bay, rather than the tabks index (`MenuView`), and the hidden-rank rule in `Ranks.cs` (names are fine outside text
players read). Left as they are: "Rooms:" at the head of a co-op comment is the project's tag for the co-op room
feature, and "Start flow 5", "S3" and "A2, H2, G3" are the labels of the owner's picks on their option pages.
When a comment names a spike, check `Spike.cs`: spikes graduate or go, and the comments that named them stay behind.
