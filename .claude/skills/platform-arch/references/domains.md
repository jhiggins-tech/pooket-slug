# The domain map

Who owns what. Every file in the repository has exactly one owner.

**The map itself is machine-checked.** The authority is
[`.claude/hierarchy/OWNERSHIP.txt`](../../../../.claude/hierarchy/OWNERSHIP.txt) — a list of `domain path` claims —
and `tools/check_ownership.py` checks that no file has two owners, that none has none, and that no claim is stale.
This page is the reasoning; the file is the map.

**Read this when** you're taking a card, cutting a domain, or asking "whose is this?".

Up: [the platform architecture](../SKILL.md) · [the roles](roles.md) · [the protocols](protocols.md)

## The eight domains

| Domain | Subject | Owner of |
|---|---|---|
| `gameplay` | the rules of play | the crew's kits, the missions and their maps, the goons, the bosses, the world, the content ids |
| `den` | the den | the den's design and art, its room layout, the unlock and progression, moving around it |
| `cutscene` | the cutscenes | the scene files, the scene engine, the stage, the script's accuracy, the preview links |
| `sprites` | the art | the frames every entity needs, hand-drawn or generated, and their animation |
| `story` | the plot | what happens, in what order, and each reveal |
| `shell` | the shell | the state machine and its flows, the screens, the title, the HUD, the input |
| `net` | the co-op | the netcode, the contracts, the room, the handover, the saves, the account |
| `audio` | the sound | the chip music and synth, the sound effects, when each track plays, the pinned takes |

The four the owner named are marked **owner's** below.

A further one, `architect`, owns the five assembly definitions (the `.asmdef` files) and the build script. They are
the boundaries between Core, Net, the game and the tests, and a change to one is a change to the shape of the
system — so it is the architect's, not a domain's.

## The rule for a file

A file belongs to the domain whose **workflow** would change it. Ask: *when this file changes, who does the
thinking?* The answer is the owner. A file that two domains would change for different reasons needs splitting — that
is usually the signal that a file is doing two jobs, which the project's own rules say to fix.

## `gameplay`

The rules of play: what the crew can do, what a mission is, what a goon does, and the content that ties them
together. Rules live in `Core` with tests; the entities and views that draw them live outside.

**Core (`Assets/Scripts/Core/`)**

- `Content.cs`, `Definitions.cs`, `Tuning.cs`, `Palette.cs` — the content table and its types, the shared numbers,
  the shared colours. **The ids live here, and they are the most dangerous thing in the repository.**
- `Crew/` — `Crew.cs`, `AvatarPose.cs`, `Controls.cs`, `DownLine.cs`, `Drip.cs`, `Lives.cs`, `SoldierJump.cs`,
  `TeamSuper.cs`, `KitGuide.cs`, `<name>Tuning.cs`, `<name>TabkSpec.cs`
- `Bosses/` — `BossTankSpec.cs`, `KieTankSpec.cs`, `KcajTankSpec.cs`, `PilotBrain.cs`, `CraterGround.cs`.
  (`BossIntroShow.cs` is the cutscene domain's: the boss intro is a show.)
- `Tabks/` — `PlayerTankSpec.cs`, `CallInCoach.cs`, `Garage.cs`, `CoopTabks.cs`, `KieHomecoming.cs`
- `Missions/` — `Missions.cs`, `Level.cs`, `MissionBoard.cs`, `StartFlow.cs`, `CodaCoach.cs`, `Creamery.cs`
  (`MissionStory.cs` is the story domain's: it is the plot in code)
- `Enemies/` — `Goons.cs`, `BigScoopBrain.cs`
- `World/` — `TilePhysics.cs`, `Frost.cs`, `HazardBeat.cs`, `Backdrop.cs` (the parallax layers and which plates cover
  the screen)
- `Input/` — `SoldierInput.cs`, `ArcadePad.cs`, `Latch.cs`
- `Platform/` — everything except the netcode files: `LaunchFlags.cs`, `Spike.cs`, `PageQuery.cs`, `Sandbox.cs`,
  `Knobs.cs`, `SetupText.cs`, `Rng.cs`, `Log.cs`
- `sandbox/*.txt` — the sandbox's setups, the links that set up a map, goons, a boss and tuning without a build

**The game (`Assets/Scripts/`)**

- `World/` — every file but `NetSync.cs`, which is the netcode's: `World.cs`, `Spawner.cs`, `Combat.cs`,
  `Scoring.cs`, `Hazards.cs`, `SetPiece.cs`, `Entity.cs`, `EntityPool.cs`, `Projectile.cs`, `Effect.cs`,
  `Particle.cs`, `Pickup.cs`, `Prop.cs`, `HitFeel.cs`, `LevelView.cs`, `ScrollCamera.cs`, `BackdropView.cs`,
  `Barrel.cs`, `Checkpoint.cs`, `Lift.cs`, `Liquids.cs`, `FrostLook.cs`
- `Crew/` — every folder: `Shared/`, `Tones2/`, `Kie/`, `Kcaj/`
- `Enemies/` — `Enemies.cs`, `CreameryGoons.cs`, `BigScoop.cs`, `HoloLook.cs`, `Weasels.cs`, `Pilot.cs`, `KiePilot.cs`,
  `KcajPilot.cs`
- `Bosses/` — `TankBoss.cs`, `BossKit.cs`, `KieBoss.cs`, `KcajBoss.cs`, `HostilePills.cs`, `CraterView.cs`
- `Tabks/` — `PlayerTank.cs`, `TankLook.cs`, `TabkKit.cs`
- `Missions/One/`, `Missions/Two/` — the missions' own pieces and their art

**Tests**

- `Assets/Tests/EditMode/Bosses/` (but `BossIntroShowTests.cs`), `Crew/`, `Enemies/`, `Input/`,
  `Missions/`, `Tabks/`, `World/` — and `ContentTests.cs`, which pins the ids
- `Assets/Tests/EditMode/Platform/` — the platform's own half: `LaunchFlagsTests.cs`, `LogTests.cs`,
  `PageQueryTests.cs`, `RngTests.cs`, `SandboxTests.cs`, `SpikeTests.cs`
- `Assets/Tests/Gameplay/` — the gameplay tests, but `NetSyncTests.cs`

**The design it decides**

- `docs/design/player.md`, `kits.md`, `levels.md`, `team-supers.md`, `tanks.md` and `tabks/` (`art.md` is the
  `sprites` domain's: the map says so)

## `den` — the owner's

**The den: its design and art selection, the room layout, the room unlock and progression, and moving around it.**

The den is a screen with its own rules, its own map, its own art and its own progression. It is cut out of the
lobby's folder-level tangle, and it is cut out by *workflow*: everything whose reason to change is "the den looks
different" or "the den opens differently" belongs here.

**The code**

- `Assets/Scripts/Core/Lobby/` — `Den.cs`, `DenMap.cs`, `DenBeats.cs`, `BayMap.cs`, `RollerDoor.cs`, the outer rooms (`RoomMap.cs`, `RopeClimb.cs`, `SwingDoor.cs`), `Den` beats, the
  bay's walker. **Not** `Profile.cs`, `Options.cs`, `Ranks.cs`, `Debrief.cs`, `CoopRoom.cs` — those are the `shell`'s
  or the `net`'s; the map says which.
- `Assets/Scripts/Ui/Den/` — every file: `DenView.cs`, `DenRoom.cs`, `DenCrew.cs`, `DenConsole.cs`, `DenInfo.cs`,
  `DenBeatView.cs`, `DenBitPlayer.cs`, `DenRollerDoor.cs`, `BayView.cs`, `BayHomecomingView.cs`
- `Assets/Scripts/Game/DenFlow.cs`, `BayFlow.cs`, and the outer rooms' `RoomsFlow.cs` and `RoomFlow.cs`
- `Assets/Resources/Den/` — the paintings, as `.bytes`

**The art** — `Assets/Resources/Den/*.bytes` (the den, the loft, the bay, the showers, the laundry, the raid den,
the street door, and the per-crewmate layers) and the house map in `docs/design/lobby/`.

**The design it decides** — `docs/design/lobby.md`, and the `lobby/` art.

**Its standing agreement with the cutscene domain** — the den is drawn on the cutscene domain's stage, with the
cutscene domain's figures and its `StagePose`. The den may not change the stage; when it needs a new figure or a
new pose, it asks the cutscene domain, and the handover says what the den must adapt to. This is in the den's
briefing as a standing rule.

**Its unlock and progression** — what opens when, and what the den shows. This is the den's own: `Profile` holds
what is saved, but the *rule* about what the den opens is the den's, and it is decided in `lobby.md`.

## `cutscene` — the owner's

**All things cutscene: the cutscene workflow, maintaining accuracy to the scripts, and creating preview links for the
cutscenes.**

The cutscene domain owns the whole loop from a line in the script to a scene on a phone: the scene files, the reader,
the engine, the stage it is drawn on, the cues, the checks, and the link that lets the owner look at a scene without a
build.

**The code**

- `scenes/*.txt` — the scene files themselves, the game's own copies
- `Assets/Scripts/Core/Story/` — every file: `SceneScript.cs`, `SceneLibrary.cs`, `Staging.cs`, `Cutscene.cs`,
  `Cues.cs`
- `Assets/Scripts/Core/Bosses/BossIntroShow.cs` — the boss intro's timeline, as a pure function of the seconds since
  it began. **The boss intro is a show**, so it is the cutscene domain's, with `Game/BossIntroFlow.cs`.
- `Assets/Scripts/Ui/Cutscene/` — every file: `CutsceneView.cs`, `CutsceneBackdrops.cs`, `CutsceneCast.cs`,
  `CutsceneEffects.cs`, `CutsceneBubbles.cs`, `CutsceneCaptions.cs`, `CutsceneCamera.cs`, `CueSounds.cs`,
  `CutsceneSnacks.cs`
- `Assets/Scripts/Stage/` — `StageKit.cs` (with `StageFigure`), `StageScreen.cs`. **The stage is the cutscene
  engine**; the den and the debrief borrow it — the den is drawn on it (`DenView` is a `StageScreen`), while the
  debrief only takes the stage's figures and colours and builds its own canvas.
- `Assets/Scripts/Game/SceneFlow.cs`, `BossIntroFlow.cs`
- `tools/scenecheck/` — the scene check, and the reader it shares with the game
- `docs/scenes.md` — how a scene is written

**The tests** — `Assets/Tests/EditMode/Story/`, and the story checks in `Missions/`.

**The design it decides** — `docs/design/cutscenes/`, the one script the scene files follow.

**The preview links** — two mechanisms, and the domain owns both:

- `?scene=<name>` plays a draft of `scenes/<name>.txt` straight away, with SKIP, the name and any problems over it.
  It never marks the scene seen, and it only plays files on the game's own site.
- The scenes go live **without a build**: `scene-files.yml` runs the scene check and puts the files up beside the
  game — the live game's for the main line, and the branch's own preview's for any other branch
  (`previews/<item-id>/?scene=<name>`). A push to `scenes/` is live in a couple of minutes.

**The script leads.** A change to a scene is decided in `docs/design/cutscenes/` first, and the scene file is
changed to match. A scene file changed without the script is an error the domain manager catches in review.

**Where the story domain comes in.** The cutscene domain owns *how* a scene plays and the *words* in it; the story
domain owns *what the story is* — the premise, the order of events, who is freed and when, and the reveals. So:

- a line of dialogue, a shot, a timing, a sound: the `cutscene` domain;
- what that line means for the plot, or which scene the plot needs next: the `story` domain, which then asks the
  cutscene domain for the scene itself.

The two overlap when a change to a scene changes the story. **The rule is which document it lands in:** a change
decided in `docs/design/cutscenes/` is the cutscene domain's; a change decided in `docs/design/story.md` is the
story domain's, and it needs a handover to the cutscene domain to carry it into the scene files.

## `sprites` — the owner's

**Creating the art for sprites, hand drawn or generated, the sprite workflow, NOT game object programming or slot
programming — just making sure all required frames are filled for entities, and the animation of frames and sprites.**

The sprites domain owns the *art* and the *art data*, and nothing about how a sprite behaves. It fills in the frames
an entity needs and keeps the animations right. It does not write a goon's brain, a kit's firing, or a slot's
behaviour.

**The code**

- `Assets/Scripts/Art/ArtData.cs` — the ASCII pixel art: every frame, as rows of characters. This is the domain's
  main file.
- `Assets/Scripts/Art/Art.cs` — the sprite builder: it turns `ArtData`'s rows into sprites, holds the palette, and
  the flash and hologram swaps. It is here because it is the art being built, not the game being played.
- `Assets/Scripts/Art/CrewArt.cs` — each crewmate's head, and the shared body and legs, and the `PlayerSkin` that
  joins them.
- `Assets/Scripts/Core/Anim/Clip.cs` and `Fx.cs` — the clip framework (frames at a rate, looped or held, with events)
  and the effect framework (the blinks, shivers and flickers). **A clip is a list of frames, so the frame contract is
  the sprites' domain**, and `Fx` is the same idea for a single frame's flicker.
- `Assets/Scripts/Core/Anim/CrewFigure.cs` — the shared figure's clips: which frames a move steps through, and at
  what rate. **The clip contract is the sprites' domain, because the frames are.** The `CrewMove`, `CrewLegs`,
  `CrewBody` and `CrewHead` enums are its vocabulary.
- `Assets/Scripts/Core/Anim/StagePose.cs` — the stage figures' poses. It is here because it names the frames.

**The assets**

- `Assets/Resources/Plates/<mission id>/` — the background plates, as `.bytes`
- `Assets/Resources/Title/` — the title logo and its parts
- `Assets/Resources/Den/` — **the den's paintings are the den's**, not the sprites'. The sprites domain makes the art
  the owner selects; the den domain decides which art the den uses and where it goes. A painting is the handover
  point between them.

**The sprite workflow** — from `docs/design/art.md`: sprites are hand-made in `ArtData` and stay that way, because
image models cannot draw a usable sprite at the game's size. A video model can animate a hand-drawn sprite, on the
owner's say-so, through the vendor repository's pipeline, and the frames are cut back to game pixels and tidied by
a person before they go in. The sprites domain runs that workflow; it does not decide which sprite to generate.

**The frame contract** — the thing the sprites domain is really responsible for: every entity that draws a figure
has frames for every state it can be in, at the right size, with the right outline, and the clip that steps through
them. **A frame that is missing is a bug in this domain**, and the check for it belongs here: which entities have
which frames, and that nothing draws a missing one. That check is a card this domain should open early, because
right now nothing in the repository tests it.

**The tests** — `Assets/Tests/EditMode/Anim/` (`ClipTests.cs`, `CrewFigureTests.cs`, `StagePoseTests.cs`) and
`Assets/Tests/EditMode/Title/` (`TitleMotionTests.cs`).

**The boundary in one line** — the sprites domain makes the art and the frames; the `gameplay` domain writes the code
that uses them. A new goon is two cards: the frames, then the brain.

## `story` — the owner's

**Maintaining consistency in plot. Changes to the story go through this domain.**

The plot is the one thing in the game that is written down in one place and spread across many others. The premise
sits in `docs/design/story.md`; the order the missions play in sits in `Missions.cs`; who each mission frees sits in
`Crew.cs`; which scene plays when sits in `MissionStory.cs`; what the game has revealed sits in `Profile.cs`; and
the scene names sit in `SceneLibrary.cs`. A change to any one of them can quietly contradict another. This domain
exists so that does not happen: **a change to the story goes through here, whoever asked for it.**

**The code**

- `Assets/Scripts/Core/Missions/MissionStory.cs` — a mission's story in code: which scenes it plays, in what order,
  which are once-only, whether it is solo only, and what it opens. This is the plot's own file, and it is this
  domain's.
- `docs/design/story.md` — the plot of record: the premise, the tone, the plot, what START plays, the coda, and
  the reveals.

**The tests** — `Assets/Tests/EditMode/Missions/MissionStoryTests.cs`.

**The other places the plot is expressed stay with their owners** — `Missions.cs` (the order and the maps) is
`gameplay`'s, `Crew.cs` (the roster) is `gameplay`'s, `SceneLibrary.cs` (the scene names) is the cutscene domain's,
and `Profile.cs` (the reveals) is the shell's. **The story domain does not own them; it owns their agreement.** That
is the whole job, and it is why the domain can exist without taking a fourth of the game's files.

**The consistency check** — this domain's reason for existing, and the card it should open early: a test that reads
the plot out of the code and checks it against `story.md`. What it checks:

- the mission order in `Missions.All` is the order the plot says, and each mission's id and net id follow the rule;
- every scene a mission's story names exists in `SceneLibrary.Names`, and every scene in the library that the plot
  mentions is reachable;
- every crewmate the plot says is freed is freed by the mission the plot says frees them (`CrewMember.FreedBy`), and
  no crewmate is freed twice;
- every reveal the plot lists is a reveal the game actually has (`Profile.Reveals`), and every reveal the game has is
  in the plot;
- the "once only" beats are marked once-only in both, and the scenes the plot retires are not played.

**Some of this is already tested, and some is not.** `MissionStoryTests` already holds a mission's scenes, its coda,
its once-only beats and when the boss reveal plays — the *behaviour* of one mission's story. What is not tested is
the *agreement*: that the plot in `story.md` and the plot in the code are the same plot. Nothing checks the mission
order against the document, nothing checks the roster against the freed list, and nothing checks the reveals against
the plot's list of them. That gap is the domain's first card, because until it is closed the plot is checked by
reading it, which is how plots rot.

**The boundary in one line** — the cutscene domain owns *how a scene plays*; this domain owns *what the story is*.
A line change is a cutscene change; a change to what a line means for the plot is a story change, and it goes
through here.

**Which document decides.** The rule that settles it: **a change decided in `docs/design/story.md` is this
domain's, and a change decided in `docs/design/cutscenes/` is the cutscene domain's.** A story change that needs a
new or altered scene is a handover: the story domain decides, the cutscene domain carries it into the scene files.

## `shell`

The shell around the game: the state machine that runs it, the screens the player sees outside a mission, and the
input that drives them.

**The code**

- `Assets/Scripts/Game/` — every flow except the cutscene's (`SceneFlow`, `BossIntroFlow`), the den's (`DenFlow`,
  `BayFlow`, `RoomsFlow`, `RoomFlow`) and the audio's (`MusicFlow`): `Game.cs`, `BoardFlow.cs`, `KitFlow.cs`, `DebriefFlow.cs`, `DemoFlow.cs`,
  `SandboxFlow.cs`
- `Assets/Scripts/Ui/` — every file except the den's and the cutscenes': `Ui.cs`, `MenuView.cs`, `HudView.cs`,
  `TitleView.cs`, `TitleLogoView.cs`, `DebriefView.cs`, `DemoClip.cs`, `Invite.cs`
- `Assets/Scripts/Input/` — `InputSources.cs`, `GameInput.cs`
- `Assets/Scripts/Core/Title/` — `TitleLogo.cs`, `TitleMotion.cs` — the logo's parts and its motion
- `Assets/Scripts/Core/Lobby/` — `Profile.cs`, `Options.cs`, `Ranks.cs`, `Debrief.cs`, `CoopRoom.cs`
- `Assets/Plugins/WebGL/PooketInvite.jslib` and `PooketDemo.jslib` — the share sheet (`Ui/Invite.cs`) and the kit
  demo recorder (`Game/DemoFlow.cs`)

**The tests** — `Assets/Tests/EditMode/Lobby/` — but the den's half of it: `CoopRoomTests.cs`, `DebriefTests.cs`,
`OptionsTests.cs`, `ProfileTests.cs`.

## `net`

The co-op: how two phones share a world, and what a player's progress outlives the session.

**The code**

- `Assets/Scripts/Net/` — every file: `NetSession.cs`, `NetReplication.cs`, `NetPrefabs.cs`, `BossNet.cs`,
  `PlayerNet.cs`, `WorldNet.cs`
- `Assets/Scripts/Core/Platform/` — the netcode half: `NetContracts.cs`, `NetIds.cs`, `NetConfig.cs`, `Rooms.cs`,
  `Handover.cs`, `DropIn.cs`, `PartnerBeacon.cs`, `WireMeter.cs`
- `Assets/Scripts/Save/` — every file: `LocalProfileStore.cs`, `OptionsStore.cs`, `CloudAccount.cs`, `SiteFile.cs`
- `Assets/Scripts/World/NetSync.cs` — the world's side of the co-op
- `Assets/Plugins/WebGL/PooketAuth.jslib` — the page's sign-in and cloud saves, the other half of `CloudAccount.cs`
- `Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion` — Fusion's own config, and it wins over the SDK's

**The tests** — `Assets/Tests/EditMode/Platform/` — but the netcode's half of it: `DropInTests.cs`,
`MasterWatchTests.cs`, `NetIdsTests.cs`, `PartnerBeaconTests.cs`, `RoomBrowserTests.cs`, `WireMeterTests.cs` — and
the co-op tests in CI (`tools/coop.mjs`), which only run
when the netcode changes or a commit asks for them.

**What it may never break** — the content ids. They ride the network, and two phones only match on an identical
build. A changed id is a changed world.

## `audio`

**The sound: the chip music and its synth, the sound effects, when each track plays, and the takes the game pins.**

Added on 25 Oct, when the project moved onto the hierarchy: the Audio and music role had a workflow of its own (the
music's notation, the sounds every new weapon and cue needs, the vendor takes and their pins), and filing it under
`gameplay` would have left that workflow with no owner.

**The code**

- `Assets/Scripts/Core/Audio/` — `Chip.cs`, `Notation.cs`, `MusicTracks.cs`, `MusicRender.cs`, `MusicMidi.cs`,
  `Wav.cs`, `Soundtrack.cs`
- `Assets/Scripts/Audio/` — `Sfx.cs`, `ChipSounds.cs`, `Music.cs`
- `Assets/Scripts/Game/MusicFlow.cs`
- `Assets/Tests/EditMode/Audio/`

**Outside the map's coverage** — `vendor.lock`, `tools/vendor_lock.sh` and `tools/vendor_fetch.sh` (the pinned takes and fetching them), and the vendor
repository's `music/`, `voice/` and `pin/`. The narrator's lines are written in the scene files (`cutscene`, from the story `story` decides) and
recorded here.

**The design it decides** — `docs/design/music.md`.

**Its seams** — every other domain asks it for a sound: a new weapon or cue names its sound in `Sfx` / `ChipSounds`,
and that is a handover to `audio`, not an edit by the asking domain.

## The seams

Four places where two domains meet and the map has to say who moves first:

| Seam | The rule |
|---|---|
| **The stage** (`Stage/`) | The `cutscene` domain owns it. The `den` asks. |
| **A new entity's art and brain** | The `sprites` domain makes the frames first; the `gameplay` domain writes the brain second, reading the sprites' briefing for the frame names. |
| **A new mission's map and goons** | The `gameplay` domain owns both — they are one subject: what the mission is. The `sprites` domain fills in any new figures they need. |
| **A change to the story** | The `story` domain owns the plot and the agreement of the places it is expressed. It does not own the files: it asks `gameplay` for a change to a mission, and `cutscene` for a change to a scene. |

And one seam that is not a file but a rule, worth saying twice because it is the one that bites: **a rule that lives
outside Core drifts.** When the `den` domain finds a rule in `DenView.cs` that belongs in `Core/Lobby/Den.cs`, it
moves it there, with tests, in the same commit. The `den` domain's own rule, and it is the one the project's working
rules single out.

## The files the map does not cover yet

The ownership check covers the game's code, its tests, its art, the scenes, the sandbox setups, the page's WebGL
plugins and the design documents the domains decide — 376 files. These are **not** covered, because they belong to
the build and the pipeline rather than to a domain:

| Path | Whose it is |
|---|---|
| `Assets/WebGLTemplates/PooketSlug/`, `Packages/`, `ProjectSettings/`, `.github/` | the build and the pipeline, the architect's own business |
| `docs/research/` (but `netcode/` and `accounts/`, which are `net`'s) | the record of how the pipeline works; the architect's |
| `CLAUDE.md`, `README.md`, `docs/design/` (the rest of it), `vendor.lock` | the owner's and the architect's |

**Widening the check is a decision, not a chore.** The check is a tool; what it covers is a decision, and the
decision should be written down rather than left to the globs in the script. When a domain is added, or a folder
that clearly belongs to one grows, the `COVERED` list in `tools/check_ownership.py` moves with it.

## Keeping this page true

The architect owns it. It changes when a file moves, when a domain grows a second subject, or when a card shows a
boundary that is wrong in practice. A boundary that is wrong in practice is worth more than a boundary that reads
well on paper: a card that crosses a line it should not have crossed is the signal, and the map is corrected after
it, not before.

**The prose and the map must agree.** This page says the same thing as `OWNERSHIP.txt` in words, and the words are
the version a person reads. A fact-check that compares the two found seven files where they had drifted apart and
one (`Core/World/Backdrop.cs`) that this page had simply forgotten. The authority is the file; the prose is not a
second source of truth, and when they disagree the prose is wrong until it is fixed.
