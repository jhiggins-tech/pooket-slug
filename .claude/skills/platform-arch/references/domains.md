# The domain map

Who owns what. Every file in the repository has exactly one owner, and this page says which. It is the map the
architect keeps true: when a file moves, this page moves with it.

**Read this when** you're taking a card, cutting a domain, or asking "whose is this?".

Up: [the platform architecture](../SKILL.md) · [the roles](roles.md) · [the protocols](protocols.md)

## The six domains

| Domain | Subject | Owner of |
|---|---|---|
| `gameplay` | the rules of play | the crew's kits, the missions and their maps, the goons, the bosses, the world, the content ids |
| `den` | the den | the den's design and art, its room layout, the unlock and progression, moving around it |
| `cutscene` | the cutscenes | the scene files, the scene engine, the stage, the script's accuracy, the preview links |
| `sprites` | the art | the frames every entity needs, hand-drawn or generated, and their animation |
| `shell` | the shell | the state machine and its flows, the screens, the title, the HUD, the input |
| `net` | the co-op | the netcode, the contracts, the room, the handover, the saves, the account |

The three the owner named are marked **owner's** below.

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
- `Bosses/` — `BossTankSpec.cs`, `KieTankSpec.cs`, `KcajTankSpec.cs`, `PilotBrain.cs`, `BossIntroShow.cs`,
  `CraterGround.cs`
- `Tabks/` — `PlayerTankSpec.cs`, `CallInCoach.cs`, `Garage.cs`, `CoopTabks.cs`, `KieHomecoming.cs`
- `Missions/` — `Missions.cs`, `Level.cs`, `MissionBoard.cs`, `MissionStory.cs`, `StartFlow.cs`, `CodaCoach.cs`,
  `Creamery.cs`
- `Enemies/` — `Goons.cs`, `BigScoopBrain.cs`
- `World/` — `TilePhysics.cs`, `Frost.cs`, `HazardBeat.cs`
- `Input/` — `SoldierInput.cs`, `ArcadePad.cs`, `Latch.cs`
- `Anim/` — `Clip.cs`, `Fx.cs` — the clip framework and the effect framework
- `Audio/` — `Chip.cs`, `Notation.cs`, `MusicTracks.cs`, `MusicRender.cs`, `MusicMidi.cs`, `Wav.cs`, `Soundtrack.cs`
- `Title/` — `TitleLogo.cs`, `TitleMotion.cs`
- `Platform/` — everything except the netcode files: `LaunchFlags.cs`, `Spike.cs`, `PageQuery.cs`, `Sandbox.cs`,
  `Knobs.cs`, `SetupText.cs`, `Rng.cs`, `Log.cs`

**The game (`Assets/Scripts/`)**

- `World/` — the whole folder: `World.cs`, `Spawner.cs`, `Combat.cs`, `Scoring.cs`, `NetSync.cs`, `Hazards.cs`,
  `SetPiece.cs`, `Entity.cs`, `EntityPool.cs`, `Projectile.cs`, `Effect.cs`, `Particle.cs`, `Pickup.cs`, `Prop.cs`,
  `HitFeel.cs`, `LevelView.cs`, `ScrollCamera.cs`, `BackdropView.cs`, `Barrel.cs`, `Checkpoint.cs`, `Lift.cs`,
  `Liquids.cs`, `FrostLook.cs`
- `Crew/` — every folder: `Shared/`, `Tones2/`, `Kie/`, `Kcaj/`
- `Enemies/` — `Enemies.cs`, `CreameryGoons.cs`, `BigScoop.cs`, `HoloLook.cs`, `Weasels.cs`, `Pilot.cs`, `KiePilot.cs`,
  `KcajPilot.cs`
- `Bosses/` — `TankBoss.cs`, `BossKit.cs`, `KieBoss.cs`, `KcajBoss.cs`, `HostilePills.cs`, `CraterView.cs`
- `Tabks/` — `PlayerTank.cs`, `TankLook.cs`, `TabkKit.cs`
- `Missions/One/`, `Missions/Two/` — the missions' own pieces and their art
- `Audio/` — `Sfx.cs`, `ChipSounds.cs`, `Music.cs`

**Tests**

- `Assets/Tests/EditMode/` — every folder except `Story/` and `Lobby/`, and except the content tests
- `Assets/Tests/Gameplay/` — the gameplay tests

**The design it decides**

- `docs/design/player.md`, `kits.md`, `levels.md`, `team-supers.md`, `tabks/`, `music.md`, `art.md` (the sprite
  side)

## `den` — the owner's

**The den: its design and art selection, the room layout, the room unlock and progression, and moving around it.**

The den is a screen with its own rules, its own map, its own art and its own progression. It is cut out of the
lobby's folder-level tangle, and it is cut out by *workflow*: everything whose reason to change is "the den looks
different" or "the den opens differently" belongs here.

**The code**

- `Assets/Scripts/Core/Lobby/` — `Den.cs`, `DenMap.cs`, `DenBeats.cs`, `BayMap.cs`, `RollerDoor.cs`, `Den` beats, the
  bay's walker. **Not** `Profile.cs`, `Options.cs`, `Ranks.cs`, `Debrief.cs`, `CoopRoom.cs` — those are the `shell`'s
  or the `net`'s; the map says which.
- `Assets/Scripts/Ui/Den/` — every file: `DenView.cs`, `DenRoom.cs`, `DenCrew.cs`, `DenConsole.cs`, `DenInfo.cs`,
  `DenBeatView.cs`, `DenBitPlayer.cs`, `DenRollerDoor.cs`, `BayView.cs`, `BayHomecomingView.cs`
- `Assets/Scripts/Game/DenFlow.cs` and `BayFlow.cs`
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
- `Assets/Scripts/Ui/Cutscene/` — every file: `CutsceneView.cs`, `CutsceneBackdrops.cs`, `CutsceneCast.cs`,
  `CutsceneEffects.cs`, `CutsceneBubbles.cs`, `CutsceneCaptions.cs`, `CutsceneCamera.cs`, `CueSounds.cs`,
  `CutsceneSnacks.cs`
- `Assets/Scripts/Stage/` — `StageKit.cs` (with `StageFigure`), `StageScreen.cs`. **The stage is the cutscene
  engine**; the den and the debrief borrow it.
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

## `sprites` — the owner's

**Creating the art for sprites, hand drawn or generated, the sprite workflow, NOT game object programming or slot
programming — just making sure all required frames are filled for entities, and the animation of frames and sprites.**

The sprites domain owns the *art* and the *art data*, and nothing about how a sprite behaves. It fills in the frames
an entity needs and keeps the animations right. It does not write a goon's brain, a kit's firing, or a slot's
behaviour.

**The code**

- `Assets/Scripts/Art/ArtData.cs` — the ASCII pixel art: every frame, as rows of characters. This is the domain's
  main file.
- `Assets/Scripts/Art/CrewArt.cs` — each crewmate's head, and the shared body and legs, and the `PlayerSkin` that
  joins them.
- `Assets/Scripts/Core/Anim/CrewFigure.cs` — the shared figure's clips: which frames a move steps through, and at
  what rate. **The clip contract is the sprites' domain, because the frames are.** The `CrewMove`, `CrewLegs` and
  `CrewBody` enums are its vocabulary.
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

**The boundary in one line** — the sprites domain makes the art and the frames; the `gameplay` domain writes the code
that uses them. A new goon is two cards: the frames, then the brain.

## `shell`

The shell around the game: the state machine that runs it, the screens the player sees outside a mission, and the
input that drives them.

**The code**

- `Assets/Scripts/Game/` — every flow except the cutscene's (`SceneFlow`, `BossIntroFlow`) and the den's (`DenFlow`,
  `BayFlow`): `Game.cs`, `BoardFlow.cs`, `KitFlow.cs`, `DebriefFlow.cs`, `MusicFlow.cs`, `DemoFlow.cs`,
  `SandboxFlow.cs`
- `Assets/Scripts/Ui/` — every file except the den's and the cutscenes': `Ui.cs`, `MenuView.cs`, `HudView.cs`,
  `TitleView.cs`, `TitleLogoView.cs`, `DebriefView.cs`, `DemoClip.cs`, `Invite.cs`
- `Assets/Scripts/Input/` — `InputSources.cs`, `GameInput.cs`
- `Assets/Scripts/Core/Title/` — `TitleLogo.cs`, `TitleMotion.cs` — the logo's parts and its motion
- `Assets/Scripts/Core/Lobby/` — `Profile.cs`, `Options.cs`, `Ranks.cs`, `Debrief.cs`, `CoopRoom.cs`

**The tests** — `Assets/Tests/EditMode/Lobby/`, `Title/`, and the content tests in `ContentTests.cs`.

## `net`

The co-op: how two phones share a world, and what a player's progress outlives the session.

**The code**

- `Assets/Scripts/Net/` — every file: `NetSession.cs`, `NetReplication.cs`, `NetPrefabs.cs`
- `Assets/Scripts/Core/Platform/` — the netcode half: `NetContracts.cs`, `NetIds.cs`, `NetConfig.cs`, `Rooms.cs`,
  `Handover.cs`, `DropIn.cs`, `PartnerBeacon.cs`, `WireMeter.cs`
- `Assets/Scripts/Save/` — every file: `LocalProfileStore.cs`, `OptionsStore.cs`, `CloudAccount.cs`, `SiteFile.cs`
- `Assets/Scripts/World/NetSync.cs` — the world's side of the co-op

**The tests** — `Assets/Tests/EditMode/Platform/` — and the co-op tests in CI (`tools/coop.mjs`), which only run
when the netcode changes or a commit asks for them.

**What it may never break** — the content ids. They ride the network, and two phones only match on an identical
build. A changed id is a changed world.

## The seams

Three places where two domains meet and the map has to say who moves first:

| Seam | The rule |
|---|---|
| **The stage** (`Stage/`) | The `cutscene` domain owns it. The `den` asks. |
| **A new entity's art and brain** | The `sprites` domain makes the frames first; the `gameplay` domain writes the brain second, reading the sprites' briefing for the frame names. |
| **A new mission's map and goons** | The `gameplay` domain owns both — they are one subject: what the mission is. The `sprites` domain fills in any new figures they need. |

And one seam that is not a file but a rule, worth saying twice because it is the one that bites: **a rule that lives
outside Core drifts.** When the `den` domain finds a rule in `DenView.cs` that belongs in `Core/Lobby/Den.cs`, it
moves it there, with tests, in the same commit. The `den` domain's own rule, and it is the one the project's working
rules single out.

## Keeping this page true

The architect owns it. It changes when a file moves, when a domain grows a second subject, or when a card shows a
boundary that is wrong in practice. A boundary that is wrong in practice is worth more than a boundary that reads
well on paper: a card that crosses a line it should not have crossed is the signal, and the map is corrected after
it, not before.
