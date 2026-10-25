# CLAUDE.md

Pooket Slug: a Metal Slug-style run-and-gun starring the crew from Pooket Tabks, for **phone browsers** (landscape,
touch; keyboard and gamepad work too). Unity 6.3 WebGL built by GameCI, hosted on GitHub Pages at
https://jhiggins-tech.github.io/pooket-slug/, with online two-player co-op on Photon Fusion 2 and optional Google
sign-in for cloud saves. Mission one is solo and teaches the game; beating it opens the crew and co-op.

## Working rules
- **Branches** (the roster, agreed 16 Oct): the main line is `claude/gallant-keller-3qzp97`, the repository's default
  branch (`main` is a stale first commit). Build each board item on `claude/<item-id>`, branched from the main line; a spike on
  `claude/spike-<name>`. Every push builds that branch's preview at
  https://jhiggins-tech.github.io/pooket-slug/previews/<branch without claude/>/ (the item's Try it link; all
  previews at `/previews/`). Only QA and release pushes the main line, once the owner approves the item on the
  Roadmap. Pull the main line before you start.
- **The Roadmap is the work queue** (https://claude.ai/artifact/1AWnQDGtUmiKMnjPgcbru9). An item going to review
  gets a page (a Claude Doc) and `page` and `line` on its card. The design is [docs/design](docs/design/README.md),
  the one source; its [history](docs/design/history.md) keeps the Roguelite and the ideas not picked.
- **Words players read**: "tabk", never tank, and "Trollogram", never hologram (code identifiers stay as they are).
  kie is said "k-eye".
- **Hidden ranks**: of the 33 ranks, players are only told of Bronze, Silver, Gold, Platinum, Diamond, Master,
  Grandmaster and Champion; the other 25 are found by reaching them. That's about spoilers for players (Jack, 17 Oct):
  never name or describe them in text players read (the game's screens, the site), but they're fine in code, tests,
  docs and commit messages.
- **Public repository**: never commit Photon SDK code (licensed; CI unpacks it from the private vendor repo),
  secrets or private-repo code.
- Spikes only answer "is it feasible"; they ride a `?spike=` flag and are never merged as they are. Rules start as
  tests against Core.
- **Leave it better when you're next in it** (Audit 2, 17 Oct): a rule living outside Core (in a view, a flow or an
  entity) moves into Core with tests the next time it changes; a big screen file (a view or flow doing several jobs) is split by part
  the next time it changes. No pass just for that.

## Checking your work
There is no Unity licence in a cloud session, so CI is the build and test run: push your branch and read the run
(EditMode tests, the WebGL build and touch play; the two-player co-op tests when you change the netcode; the kit demo
recording only on the main line, or with a "CI: demos" line in a commit message; spikes skip co-op). Before pushing, run the same checks yourself in one command:
```sh
tools/check.sh           # compile every assembly as Unity would for WebGL, Fusion's weaver, the EditMode tests,
                         # the scene check and the docs link check (about 30 s)
tools/check.sh compile   # only the type-check
tools/check.sh docs      # only the docs link check: every relative link in a .md resolves
```
The first run in a session downloads the Unity kit for the project's Unity version and the Photon SDK from the private
vendor repository (about a minute). After a Unity upgrade, run the vendor repository's *Unity kit* workflow with the
new version first. The scripts underneath are in [docs/research/verification](docs/research/verification/README.md).

github.io is blocked from cloud sessions: to smoke-test a deploy, fetch the `gh-pages` branch and serve it locally
(`smoke.mjs`; `tools/touch.mjs` plays a build by touch and `tools/coop.mjs` is the two-browser co-op test, both run by CI). New `.cs` files go in the folder for what they're about (Core's tests in the matching test folder) and need a `.meta`, as does a new folder
(`tools/new_meta.py`).

## Map
All game code is C# under `Assets/Scripts`, spawned from code: no scenes (the build makes an empty one), no prefabs.
Each file starts with a summary comment saying what it's for.

| Where | What |
|---|---|
| `Core/` (assembly `PooketSlug.Core`, no references) | The rules, as plain C#, tested; its folders mirror the game's. At its root: `Content.cs` (soldiers, weapons, enemies, pickups as definitions with stable ids), `Definitions.cs` (their types), `Tuning.cs` (shared numbers), `Palette.cs` (shared colours: gold, the players' colours) |
| `Core/Crew/` | `Crew.cs` (who you play, who's freed), `<name>Tuning.cs` (each one's numbers), `Tones2TabkSpec` / `KieTabkSpec` / `KcajTabkSpec` (their tabks' guns), `Controls.cs` (the kit's meters), `Drip` (a flow spread over frames), `SoldierJump` (coyote time, a buffered press, dropping through a bridge, the short hop: your soldier's and a pilot's), `KitGuide` (the ⓘ screen's moves and demo scripts), `TeamSuper`, `Lives`, `DownLine` (what the HUD says while you're down), `AvatarPose` |
| `Core/Bosses/` | `BossTankSpec` (the artillery boss tabk), `KieTankSpec` / `KcajTankSpec` (their tabks' charged shots), `PilotBrain`, `BossIntroShow`, `CraterGround` |
| `Core/Tabks/` | `PlayerTankSpec`, `CallInCoach`, `Garage` (the bay's tabks: `TabkCards`, who's won which, the loaded one, and the drone's swaps and shuffles as a `DroneShow`), `KieHomecoming` (kie's tabk coming home: the bay's beat), `CoopTabks` (one of each tabk in co-op: the partner's is taken, and who keeps it in a clash) |
| `Core/Missions/` | `Level.cs` + `Missions.cs` (maps as text), `MissionStory` (a mission's scenes and once-only beats), `MissionBoard` (the mission screen, and whether a mission's cleared), `StartFlow` (what START plays: Mission one until it's beaten, then the den), `CodaCoach`, `Creamery` (Mission two's hazards: belts, vents, droppers, its boxes and tanks) |
| `Core/Story/` | `SceneScript` (the scene files' reader), `SceneLibrary` (which copy plays, and the check), `Staging`, `Cutscene.cs` (the player) + `Cues.cs` (the cue ids) |
| `Core/Lobby/` | `Den`, `DenMap` (the den's floors, ladder and places, and walking round them), `BayMap` (the tabk bay's decks, spots and lift, and `BayWalker`), `RollerDoor` (the den's door to it, rolling up and down), `RoomMap` (the house's outer rooms, each behind a door), `RopeClimb` (the bunker's rope), `SwingDoor` (a door that swings, like the kitchen's back door), `DenBeats` (the first visit and a crewmate moving in: when they're due, their timing), `Debrief` (XP), `Ranks`, `Profile` (what's saved, and reveals), `Options`, `CoopRoom` (the one co-op room, MATES) |
| `Core/Audio/` | `MusicTracks` / `Notation` / `Chip` (the chip music, its notation and the synth), `Soundtrack` (when a mission's music changes), `MusicRender` (a track into samples), `Wav`, `MusicMidi` |
| `Core/Enemies/` | Mission two's goons: `Goons.cs` (`ShieldSpec`: what the shield stops; `GrenadierSpec`: the lob), `BigScoopBrain` (the miniboss's moves) |
| `Core/Title/` | `TitleLogo` (the title logo's parts, `Resources/Title/layout.txt`), `TitleMotion` (its entrance, hover and glint, and when its sounds play) |
| `Core/Anim/` | `Clip` (frames at a rate, looped or held, with events; `Clips`: who runs at what rate), `Fx` (blinks, shivers and flickers: `Pulse`, and the named ones such as `TrolloFlicker`), `StagePose` (a stage soldier's legs and top, and the cutscenes' pose names, drawn by `StageFigure.Pose`), `CrewFigure` (the playable crew's one set of clips: idle, run, jump, crouch, down, repair; and the figure's layers: shared legs and body, each crewmate's own head) |
| `Core/Input/`, `Core/World/` | `ArcadePad` (the touch pad's logic), `SoldierInput`, `Latch` (a press or tap held till a screen's `Take*` takes it); `TilePhysics`, `Frost` (frozen by nitrogen), `HazardBeat` (a timed hazard's on, warning and cycle, and Mission one's: `BaseHazards`), `Backdrop` (the parallax layers, and which plates cover the screen) |
| `Core/Platform/` | `LaunchFlags` (`?test=`, `?unlock=` and the rest), `Spike.cs` (`?spike=` flags), `PageQuery`, `Sandbox` + `Knobs` + `SetupText`, `Rng`, `Log`; co-op's side: `NetContracts` / `NetIds` / `NetConfig` (interfaces and ids), `Rooms` (a room's listing), `Handover` (the world moving to the partner), `DropIn` (joining a mission under way, own scores), `PartnerBeacon` (the partner arrow and warnings), `WireMeter` (the kinds of message a phone sends its partner) |
| `Game/` | `Game.cs`: `Bootstrap` and `Game`, the state machine (`GameState`: Title, Settings, Lobby, Den, Cutscene, Playing, GameOver, Victory, Demo), the world, the launch flags and the mission's story beats (from its `MissionStory`); then a flow per part of the game it hands frames to: `SceneFlow` (cutscenes, the site's copies, `?scene=` drafts), `DenFlow`, `KitFlow` (the ⓘ kit screen), `BoardFlow` (the mission screen), `DemoFlow` (`?demo=`), `SandboxFlow` (`?sandbox=`), `BossIntroFlow`, `MusicFlow`, `DebriefFlow`, `BayFlow` (the tabk bay), `RoomsFlow` + `RoomFlow` (the house's outer rooms, each its own screen) |
| `World/` | One play-through (`World.cs`), rebuilt on restart, split by concern: `ScrollCamera` + `BackdropView` (the parallax: painted plates from `Resources/Plates/<mission id>/`, else drawn), `Spawner` (what each brain and map marker makes, as two tables), `Combat` (shots, hits, who owns damage), `Scoring`, `NetSync` (the world's side of co-op), `Hazards` (the fence, vents and icicles), `SetPiece` (the base of what stands in the level until it's shot: `Barrel`, the missions' boxes, projectors, tower and gate), `Lift`, `Checkpoint`, `Liquids` (tones2's); and what every play-through is made of: `Entity` / `EntityPool` (an `Actor` falls and walks with `Fall` and `Walk`), `Projectile`, `Effect`, `Particle`, `Pickup`, `Prop`, `HitFeel`, `LevelView` |
| `Crew/` | `Shared/` (`Player`: your soldier, `CrewKit`: the kit base every crewmate's kit fills and the soldier calls, with `KitCopy` for a partner's, `RemotePlayer`: your partner's, `PlayerVisual`, `TeamSuperShow` + `TeamActs`: each combo's show), then one folder per crew member: `Tones2/` (`Tones2Kit`, `Tones2Art`: his liquids, `Tones2Tabk`: his tabk's guns), `Kie/` (`KieKit`, `KieRoulette`, `KieTabk`: his tabk's guns, `KieTabkArt`: The Projectionist), `Kcaj/` (`KcajKit`: the cones; `KcajBeam`: Hyperfixate; `KcajPills`: Unmedicated's rain and `PillView`; `KcajCopy` for a partner's, his or his tabk's; `KcajArt`: his sprites, and every pill placed one way; `KcajTabk`: his tabk's guns, `KcajTabkArt`: SOFT SERVE) |
| `Enemies/` | `Enemies` (`Enemy` and the goons: `Rifleman`, `Runner`, `Drone`), `CreameryGoons` (`ShieldSoldier`, `Grenadier`), `BigScoop` (Mission two's miniboss), `HoloLook` (Trollogram goons), `Weasels`, `Pilot` (a boss's Trollogram pilot on foot, playing a crewmate's kit: `KiePilot` with `PilotDecoy`, `KcajPilot`) |
| `Bosses/` | `TankBoss` (the artillery boss tabk) + `BossKit` (the base of a crew member's attacks on it: `KieBoss`, `KcajBoss`; `BossKits` picks one by `BossTabk`), `HostilePills` (kcaj's pill rain, from his tabk or his pilot), `CraterView` (the diggable arena spike) |
| `Tabks/` | `PlayerTank` (your tabk; `Fire` spends a gun's round on a press, or clicks when it's out) + `TankLook`, `TabkKit` (a crew member's tabk: its look, gun pivot and guns) |
| `Missions/One/` | Mission one's own pieces: the coda's `BackGate`, `ToppleTower` (the watchtower in the road), `ExitGate` (Design's sprite: `ExitGateArt`) and `SleepingKie`, drawn by `CodaArt` |
| `Missions/Two/` | Mission two's (the creamery): `CreameryHazards` (belt treads, nitrogen vents, scoop droppers, the cold store's doors, the corridor's `FogBank`), `ConeBox`, `Projector` (a Trollogram projector: shot out, its goons go), drawn by `CreameryArt`; the nitrogen tank is a `Barrel`, and `FrostLook` (in `World/`) is the ice on whoever's frozen |
| `Stage/` | `StageKit`: the shared stage for cutscenes, the den and the debrief; `StageScreen`: the base of a screen on it (the den, the tabk bay): its canvas, and a tap in art pixels |
| `Art/`, `Audio/` | `Art` + `ArtData` (ASCII pixel art; `Art.Renderer` makes a sprite's child object), `CrewArt` (each crewmate's look: their head, and any body or legs part of their own, built into a `PlayerSkin`); `Sfx` + `ChipSounds`, `Music` |
| `Ui/` | uGUI built in code (`Ui.cs`, with the shared helpers: `Cover`, `Tap`, `Stretch`; screens hold a press for their `Take*` in a `Latch`): `HudView`, `MenuView`, `TitleView` (the splash: logo, colour ramp, START and SETTINGS) + `TitleLogoView` (the logo in parts, animated), `DebriefView`, `DemoClip`, `Invite` (sharing an invite link); `Cutscene/` (`CutsceneView` and its parts: backdrops, cast, effects, bubbles, captions, camera, cue sounds, `CutsceneSnacks`: tones2's crisp packet and crisps) and `Den/` (`DenView`, the lobby, and its parts: room, crew, bits, TV and buttons, `DenBeatView` its beats; `BayView`, the tabk bay through its roller door, with `BayHomecomingView`; `DenRollerDoor`, `DenTrapdoor` (the trapdoor to the basement); `RoomView`, an outer room, with `RoomBackDoor`, the kitchen's back door) |
| `Input/` | `InputSources` (a keyboard, a gamepad or the touch pad) into one `SoldierInput` a frame, and `GameInput` (the menu and system buttons) |
| `Net/` (assembly `PooketSlug.Net`) | Photon Fusion 2, Shared Mode: `NetSession` (joining the co-op room, to play or watch), `NetReplication` (`PlayerNet`, `WorldNet`, `BossNet`), `NetPrefabs` (network objects built in code) |
| `Save/` | `LocalProfileStore` / `OptionsStore` (PlayerPrefs), `CloudAccount` (sign-in and cloud saves through the page's `pooket-auth.js`), `SiteFile` (a text file fetched from beside the game) |
| `Assets/Editor/WebGLBuild.cs` | The build: the scene, the WebGL template (`Assets/WebGLTemplates/PooketSlug`), the commit and secrets baked in |
| `Assets/Tests/EditMode/` | NUnit tests against Core, in the same folders as Core, run by CI before every build |
| `Assets/Tests/Gameplay/` | PlayMode tests (assembly `PooketSlug.Game.Tests`): a real `World`, ticked frame by frame (`GameplayFixture`), for combat, the world, the spawner, set pieces, bosses and `NetSync`; CI runs them with the EditMode tests, Unity only |
| `tools/` | `check.sh` (the local checks), `pages_deploy.sh` (the gh-pages deploy), `slug.sh` (a branch's preview folder), `vendor_fetch.sh` + `vendor_lock.sh` (the vendor repo's sounds, as the root's `vendor.lock` pins them), `check_scenes.sh` (the scene check), `coop.mjs` (co-op tests), `touch.mjs` (touch play), `demos.mjs` (kit demo clips), `unpack_unitypackage.py`, `new_meta.py`, `title_logo.py` (the splash's logo and its parts) |
| `docs/` | `design/`: the game design. `research/`: how the pipeline, netcode, accounts and verification work (`history/`: finished research). [scenes.md](docs/scenes.md): writing cutscenes; [sandbox.md](docs/sandbox.md): the sandbox's setups |

## How the game fits together
- **Flow**: `Game` owns the state and one `World`, and hands each frame to the flow for what's up (`Game/*Flow.cs`).
  The title, a splash with START and SETTINGS only (`TitleView`) → until Mission one's beaten, START plays the opening
  and Mission one (`StartFlow`); after, START and the debrief go to the den, where missions, the crew and co-op open up as they're unlocked (progressive disclosure, `Profile`
  reveals) → playing → the debrief, XP and rank, and back to the den.
- **Core and the rest**: rules live in Core with tests; entities and views draw, play sounds and call into Core.
  Core knows nothing of Unity's scene or the network.
- **Co-op** ([netcode](docs/research/netcode/README.md)): Shared Mode, so every client simulates its own soldier and
  the room's master client runs the shared world (enemies, the boss, pickups, scroll). The others report their hits.
  World talks to the network only through `INetLink`. Things share ids by rule (`NetIds`: a level spawn's id is its
  index). Only identical builds match, so each preview is its own co-op world.
- **Cutscenes** are text files in `scenes/` ([how to write them](docs/scenes.md)), read by `SceneScript` and staged by
  `CutsceneView` (`Ui/Cutscene/`). `scene-files.yml` checks them and puts them up beside the game without a build; the game fetches
  them at start-up (its built-in copy is the fallback) and `?scene=<name>` plays a draft. The narrator's audio is made
  in the vendor repository (`voice/narration.json`) and fetched by CI; lengths come from the clips.
- **Launch flags**, read once as the game starts: `?spike=` (spikes, `Spike.cs`) and the rest into `LaunchFlags` (Core, tested): `?test=` (crate, tank, kietabk, softserve, 1up, pilot,
  weak, team, kcajtank, boss, reveal, coda, debrief, scene: jump straight to a part), `?unlock=` (coop, tank,
  crew), `?demo=` (a kit move's scripted demo; `all` records them for CI), `?room=` (an invite), `?log=` (log levels),
  `?mission=2` (START plays that mission).
- **Sound and music**: Tabks' 8-bit recipes on the `Chip` synth, rendered at startup; the announcer, narration and
  orchestral music are WAVs CI fetches from vendor releases into `Resources` (`.bytes`, made into clips in code).
  `vendor.lock` pins each by hash, so a new take reaches the game only when a branch pins it (`tools/vendor_lock.sh`),
  and the main line fails rather than ship without one.

## Recipes
**Add a room to the house**: its own screen behind a door, like the tabk bay: the workflow is [docs/design/lobby/rooms.md](docs/design/lobby/rooms.md) (design it in the map, paint it with OpenRouter, then `RoomMap`/`RoomWalker` in Core, `RoomView` and `RoomFlow`, and a `DenPlace` for its door).

**Add a cutscene**: its script in `docs/design/cutscenes/README.md` (the script leads), then a file in `scenes/` ([docs/scenes.md](docs/scenes.md)) and its name in `SceneLibrary`, cues from
`Cues` (`CueTests` holds every cue to having an effect), narrator lines in the vendor repo's `voice/narration.json`,
then where it plays in `Game`.

**Add a weapon**: its `WeaponDef` (and `ProjectileDef`, if it fires one) in `Content`, with the next unused id
(ids ride the network: never change or reuse one), its sprite in `Art` / `ArtData` (keyed by the projectile's id),
its sound in `Sfx` / `ChipSounds`, and the kit that fires it.

**Add a sound cue**: in a scene, its constant and effect in `Cues` (a new `CueSound`: its clip in `CueSounds`); in the boss intro, its
place on the `Cue` timeline in `BossIntroShow`. Either way its sound goes in `Sfx` / `ChipSounds`.

**Add a mission** (Mission two touched about 40 files; `MissionsTests`, `ContentTests`, `SpawnerTests` and the scene check hold
much of it):
- Core: a `MissionDef` in `Missions` (id `mission-<N>`, the next `NetId`) in `Missions.All`, its map rows
  (`Level.Markers` says what each character places; new ones per *Add a goon* and *Add a hazard*), its boss (`boss:` a
  `BossTabk`, attacks from its row in `BossKits`) and pilot (`pilot:` an `EnemyDef`, see *Add a crew member*), its
  pickups from the shared table; rules of its own in `Core/Missions/<Place>.cs` (as `Creamery`) with tests.
- What it opens: `MissionBoard` (the mission screen, hard), who it frees (`Crew`), the tabk it wins (`Garage`).
- Story (*Add a cutscene*): briefing and freeing scenes in `MissionStory` and `SceneLibrary`, the boss intro
  (`BossIntroShow`), its start line in `Cues` (and the announcer's take in the vendor repo); `levels.md` and
  `story.md` say whether it's solo.
- Its own pieces in `Missions/<N>/` (art in `<Place>Art`), painted plates in `Resources/Plates/<mission id>/`,
  sounds in `Sfx` / `ChipSounds`; `?mission=N` and the sandbox's `mission` find it by its id.

**Add a goon** (or anything that's shot, like a set piece): its `EnemyBrain` and an `EnemyDef` row in `Content` with
the next unused id (ids ride the network), its `SpawnKind` and map character (`marker:`, `map:`; left out for one only
made mid-game), and `real: true` unless it's a Trollogram; then its class (a goon in `Enemies/`, a set piece a
`SetPiece` in its mission's folder) and its row in `Spawner`'s brain table. Its rules and numbers go in Core with tests,
its art in `Art` / `ArtData`, its sounds in `Sfx` / `ChipSounds`.

**Add a hazard** (or scenery that isn't shot): a `SpawnKind`, its character in `Level.Markers` (`ContentTests`) and
its row in `Spawner`'s scenery table (`SpawnerTests`); its timing and numbers in Core with tests, from
`World.Elapsed` so every phone agrees without the network (as `BaseHazards`, `Creamery`); then a `Hazard` in its
mission's folder (Mission one's are in `World/Hazards.cs`) that hurts or freezes only this phone's soldier (goons:
the authority only) and plays its sounds only on screen; art in its mission's `*Art`, sounds in `Sfx` / `ChipSounds`.

**Add a crew member** (kcaj touched about 55 files):
- Core: a `SoldierDef` in `Content` (stable id: `ContentTests`), numbers in `Core/Crew/<Name>Tuning.cs` with tests,
  their place in `Crew` (who frees them), team supers (`TeamSuper`), kit screen moves and demos (`KitGuide`), and
  `KitMove` ids for moves a partner copies (`NetContracts`, `WireMeter`).
- Their look in `CrewArt`: their own head over the shared body and legs, which brings every crew clip (`CrewFigure`) with it.
- Kit in `Crew/<Name>/`: a `CrewKit` (picked in `CrewKit.For`), `<Name>Copy` for a partner's (`KitCopy.For`,
  `RemotePlayer`), art in `<Name>Art` (registered in `Art`), each combo's show in `TeamActs`.
- Their tabk: `<Name>TabkSpec` (Core) + `<Name>Tabk` and `<Name>TabkArt`, a card in `Garage`, a `TabkKit`.
- As a boss: their boss tabk's charged shot (`Core/Bosses/<Name>TankSpec`), their attacks on it (a `BossKit`, row in
  `BossKits`), a pilot (`Enemies/<Name>Pilot`, `PilotBrain`).
- Around them: a den station (`Ui/Den/DenRoom`), the cutscenes' cast (`Staging`, `CutsceneCast`), the launch flags
  and sandbox if they need a way in (`LaunchFlags`, `Sandbox`, `Knobs`), sounds in `Sfx` / `ChipSounds`, and
  `docs/design/kits.md`.

**Add a spike**: a key in `Spike.cs` (solo only unless it says otherwise), on branch `claude/spike-<name>`. Its
preview link with `?spike=<key>` is the Try it link.

**Change what crosses the network**: the structs in `Net/` and `Core/Platform/NetContracts.cs` (a crewmate's move that isn't a shot is a `KitEvent`); content ids never change or
get reused (the tests pin them).

## Invariants and gotchas
- Gameplay randomness only through a seeded `Rng`; cosmetic effects use their own.
- A blink, flicker or walk cycle goes through `Fx` or a `Clip`, not an inline `(int)(t * hz) % n`; a figure on a stage is posed with `StageFigure.Pose`; a crewmate's figure plays `CrewFigure`'s clips (a new shared move is a clip there and body frames in `ArtData`).
- The build is uncompressed: Pages gzips it on the fly ([why](docs/research/build-pipeline/github-pages.md)).
- Docs-only pushes (`**.md`, `docs/**`) don't build: a redeploy changes the build version co-op players must share.
- Fusion's own config (`NetworkProjectConfig.fusion`) is committed and wins over the one the SDK ships.
