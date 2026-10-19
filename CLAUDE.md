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
(EditMode tests, the WebGL build, the kit demo recording and the two-player co-op tests; spike branches skip the
last two). Before pushing, run the same checks yourself in one command:
```sh
tools/check.sh           # compile every assembly as Unity would for WebGL, run Fusion's weaver, run the EditMode tests
tools/check.sh compile   # only the type-check
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
| `Core/Crew/` | `Crew.cs` (who you play, who's freed), `<name>Tuning.cs` (each one's numbers), `Tones2TabkSpec` (his tabk's guns), `Controls.cs` (the kit's meters), `Drip` (a flow spread over frames), `KitGuide` (the ⓘ screen's moves and demo scripts), `TeamSuper`, `Lives`, `DownLine` (what the HUD says while you're down), `AvatarPose` |
| `Core/Bosses/` | `BossTankSpec` (the artillery boss tabk), `KieTankSpec` / `KcajTankSpec` (their tabks' charged shots), `PilotBrain`, `BossIntroShow`, `CraterGround` |
| `Core/Tabks/` | `PlayerTankSpec`, `CallInCoach`, `Garage` (the bay's tabks: `TabkCards`, who's won which, the loaded one, and the drone's swaps and shuffles as a `DroneShow`) |
| `Core/Missions/` | `Level.cs` + `Missions.cs` (maps as text), `MissionStory` (a mission's scenes and once-only beats), `MissionBoard` (the mission screen, and whether a mission's cleared), `StartFlow` (what START plays: Mission one until it's beaten, then the den), `CodaCoach`, `Creamery` (Mission two's hazards: belts, vents, droppers, its boxes and tanks) |
| `Core/Story/` | `SceneScript` (the scene files' reader), `SceneLibrary` (which copy plays, and the check), `Staging`, `Cutscene.cs` (the player) + `Cues.cs` (the cue ids) |
| `Core/Lobby/` | `Den`, `DenMap` (the den's floors, ladder and places, and walking round them), `BayMap` (the tabk bay's decks, spots and lift, and `BayWalker`), `RollerDoor` (the den's door to it, rolling up and down), `DenBeats` (the first visit and a crewmate moving in: when they're due, their timing), `Debrief` (XP), `Ranks`, `Profile` (what's saved, and reveals), `Options`, `CoopRoom` (the one co-op room, MATES) |
| `Core/Audio/` | `MusicTracks` / `Notation` / `Chip` (the chip music, its notation and the synth), `Soundtrack` (when a mission's music changes), `MusicRender` (a track into samples), `Wav`, `MusicMidi` |
| `Core/Enemies/` | Mission two's goons: `Goons.cs` (`ShieldSpec`: what the shield stops; `GrenadierSpec`: the lob), `BigScoopBrain` (the miniboss's moves) |
| `Core/Title/` | `TitleLogo` (the title logo's parts, `Resources/Title/layout.txt`), `TitleMotion` (its entrance, hover and glint, and when its sounds play) |
| `Core/Input/`, `Core/World/` | `ArcadePad` (the touch pad's logic), `SoldierInput`; `TilePhysics`, `Frost` (frozen by nitrogen), `Backdrop` (the parallax layers, and which plates cover the screen) |
| `Core/Platform/` | `LaunchFlags` (`?test=`, `?unlock=` and the rest), `Spike.cs` (`?spike=` flags), `PageQuery`, `Sandbox` + `Knobs` + `SetupText`, `Rng`, `Log`; co-op's side: `NetContracts` / `NetIds` / `NetConfig` (interfaces and ids), `Rooms` (a room's listing), `Handover` (the world moving to the partner), `DropIn` (the drop-in spike), `PartnerBeacon` (the partner arrow spike), `WireMeter` (the kinds of message a phone sends its partner) |
| `Game/` | `Game.cs`: `Bootstrap` and `Game`, the state machine (`GameState`: Title, Settings, Lobby, Den, Cutscene, Playing, GameOver, Victory, Demo), the world, the launch flags and the mission's story beats (from its `MissionStory`); then a flow per part of the game it hands frames to: `SceneFlow` (cutscenes, the site's copies, `?scene=` drafts), `DenFlow`, `KitFlow` (the ⓘ kit screen), `BoardFlow` (the mission screen), `DemoFlow` (`?demo=`), `SandboxFlow` (`?sandbox=`), `BossIntroFlow`, `MusicFlow`, `DebriefFlow`, `BayFlow` (the tabk bay) |
| `World/` | One play-through (`World.cs`), rebuilt on restart, split by concern: `ScrollCamera` + `BackdropView` (the parallax: painted plates from `Resources/Plates/<mission id>/`, else drawn), `Spawner`, `Combat` (shots, hits, who owns damage), `Scoring`, `NetSync` (the world's side of co-op), `Hazards` (the fence, vents and icicles), `Barrel`, `Lift`, `Checkpoint`, `Liquids` (tones2's); and what every play-through is made of: `Entity` / `EntityPool`, `Projectile`, `Effect`, `Particle`, `Pickup`, `Prop`, `HitFeel`, `LevelView` |
| `Crew/` | `Shared/` (`Player`: your soldier, `CrewKit`: the kit base every crewmate's kit fills and the soldier calls, with `KitCopy` for a partner's, `RemotePlayer`: your partner's, `PlayerVisual`, `TeamSuperShow` + `TeamActs`: each combo's show), then one folder per crew member: `Tones2/` (`Tones2Kit`, `Tones2Art`: his liquids, `Tones2Tabk`: his tabk's guns), `Kie/` (`KieKit`, `KieRoulette`), `Kcaj/` (`KcajKit`: the cones, the beam and the pills, with `KcajCopy` for a partner's) |
| `Enemies/` | `Enemies` (`Enemy` and the goons: `Rifleman`, `Runner`, `Drone`), `CreameryGoons` (`ShieldSoldier`, `Grenadier`), `BigScoop` (Mission two's miniboss), `HoloLook` (Trollogram goons), `Weasels`, `Pilot` |
| `Bosses/` | `TankBoss` (the artillery boss tabk) + `BossKit` (a crew member's attacks on it: `KieBoss`, `KcajBoss`), `CraterView` (the diggable arena spike) |
| `Tabks/` | `PlayerTank` (your tabk) + `TankLook`, `TabkKit` (a crew member's tabk) |
| `Missions/One/` | Mission one's own pieces: the coda's `BackGate`, `ToppleTower` (the watchtower in the road), `ExitGate` and `SleepingKie`, drawn by `CodaArt` |
| `Missions/Two/` | Mission two's (the creamery): `CreameryHazards` (belt treads, nitrogen vents, scoop droppers, the cold store's doors, the corridor's `FogBank`), `ConeBox`, `Projector` (a Trollogram projector: shot out, its goons go), drawn by `CreameryArt`; the nitrogen tank is a `Barrel`, and `FrostLook` (in `World/`) is the ice on whoever's frozen |
| `Stage/` | `StageKit`: the shared stage for cutscenes, the den and the debrief |
| `Art/`, `Audio/` | `Art` + `ArtData` (ASCII pixel art; `Art.Renderer` makes a sprite's child object); `Sfx` + `ChipSounds`, `Music` |
| `Ui/` | uGUI built in code (`Ui.cs`, with the shared helpers: `Cover`, `Tap`, `Stretch`): `HudView`, `MenuView`, `TitleView` (the splash: logo, colour ramp, START and SETTINGS) + `TitleLogoView` (the logo in parts, animated), `DebriefView`, `DemoClip`, `Invite` (sharing an invite link); `Cutscene/` (`CutsceneView` and its parts: backdrops, cast, effects, bubbles, captions, camera, cue sounds) and `Den/` (`DenView`, the lobby, and its parts: room, crew, bits, TV and buttons, `DenBeatView` its beats; `BayView`, the tabk bay through its roller door, `DenRollerDoor`) |
| `Input/` | `InputSources` (a keyboard, a gamepad or the touch pad) into one `SoldierInput` a frame, and `GameInput` (the menu and system buttons) |
| `Net/` (assembly `PooketSlug.Net`) | Photon Fusion 2, Shared Mode: `NetSession` (joining the co-op room, to play or watch), `NetReplication` (`PlayerNet`, `WorldNet`, `BossNet`), `NetPrefabs` (network objects built in code) |
| `Save/` | `LocalProfileStore` / `OptionsStore` (PlayerPrefs), `CloudAccount` (sign-in and cloud saves through the page's `pooket-auth.js`), `SiteFile` (a text file fetched from beside the game) |
| `Assets/Editor/WebGLBuild.cs` | The build: the scene, the WebGL template (`Assets/WebGLTemplates/PooketSlug`), the commit and secrets baked in |
| `Assets/Tests/EditMode/` | NUnit tests against Core, in the same folders as Core, run by CI before every build |
| `Assets/Tests/Gameplay/` | PlayMode tests (assembly `PooketSlug.Game.Tests`): a real `World`, ticked frame by frame (`GameplayFixture`), for combat, the world and `NetSync`; CI runs them with the EditMode tests, Unity only |
| `tools/` | `check.sh` (the local checks), `pages_deploy.sh` (the gh-pages deploy), `slug.sh` (a branch's preview folder), `vendor_fetch.sh` + `check_scenes.sh` (the vendor repo's sounds, the scene check), `coop.mjs` (co-op tests), `touch.mjs` (touch play), `demos.mjs` (kit demo clips), `unpack_unitypackage.py`, `new_meta.py`, `title_logo.py` (the splash's logo and its parts) |
| `docs/design/` | The game design. `docs/research/`: how the pipeline, netcode, accounts and verification work, and [lessons learned](docs/research/lessons-learned.md) |

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
- **Launch flags**, read once as the game starts: `?spike=` (spikes, `Spike.cs`) and the rest into `LaunchFlags` (Core, tested): `?test=` (crate, tank, 1up, pilot,
  weak, team, kcajtank, boss, reveal, coda, debrief, scene: jump straight to a part), `?unlock=` (coop, tank,
  crew), `?demo=` (a kit move's scripted demo; `all` records them for CI), `?room=` (an invite), `?log=` (log levels),
  `?mission=2` (START plays that mission).
- **Sound and music**: Tabks' 8-bit recipes on the `Chip` synth, rendered at startup; the announcer, narration and
  orchestral music are WAVs CI fetches from vendor releases into `Resources` (`.bytes`, made into clips in code).

## Recipes
**Add a cutscene**: its script in `docs/design/cutscenes.md` (the script leads), then a file in `scenes/` ([docs/scenes.md](docs/scenes.md)) and its name in `SceneLibrary`, cues from
`Cues` (`CueTests` holds every cue to having an effect), narrator lines in the vendor repo's `voice/narration.json`,
then where it plays in `Game`.

**Add a weapon**: its `WeaponDef` (and `ProjectileDef`, if it fires one) in `Content`, with the next unused id
(ids ride the network: never change or reuse one), its sprite in `Art` / `ArtData` (keyed by the projectile's id),
its sound in `Sfx` / `ChipSounds`, and the kit that fires it.

**Add a sound cue**: in a scene, its constant and effect in `Cues` (a new `CueSound`: its clip in `CueSounds`); in the boss intro, its
place on the `Cue` timeline in `BossIntroShow`. Either way its sound goes in `Sfx` / `ChipSounds`.

**Add a mission**: a `MissionDef` in `Missions` (map rows: `Level.cs` says what each character is), its story
(`MissionStory`: its scenes, coda, solo only), its spawns, and where it opens (`Profile`, `MissionBoard`).

**Add a crew member**: a `SoldierDef` in `Content` (stable id), their numbers in `Core/Crew/<Name>Tuning.cs` with tests,
their kit in `Crew/<Name>/` (a `CrewKit`, picked in `CrewKit.For`; moves a partner copies go as a `KitMove`), art in `ArtData`, sounds in `Sfx` / `ChipSounds`, their place in `Crew`, their kit
screen demos (`KitGuide`), team supers (`TeamSuper`), and their boss tabk if they have one (`BossKit`).

**Add a spike**: a key in `Spike.cs` (solo only unless it says otherwise), on branch `claude/spike-<name>`. Its
preview link with `?spike=<key>` is the Try it link.

**Change what crosses the network**: the structs in `Net/` and `Core/Platform/NetContracts.cs` (a crewmate's move that isn't a shot is a `KitEvent`); content ids never change or
get reused (the tests pin them).

## Invariants and gotchas
- Gameplay randomness only through a seeded `Rng`; cosmetic effects use their own.
- The build is uncompressed: Pages gzips it on the fly ([why](docs/research/build-pipeline/github-pages.md)).
- Docs-only pushes (`**.md`, `docs/**`) don't build: a redeploy changes the build version co-op players must share.
- Fusion's own config (`NetworkProjectConfig.fusion`) is committed and wins over the one the SDK ships.
