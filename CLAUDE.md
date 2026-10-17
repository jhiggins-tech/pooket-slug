# CLAUDE.md

Pooket Slug: a Metal Slug-style run-and-gun starring the crew from Pooket Tabks, for **phone browsers** (landscape,
touch; keyboard and gamepad work too). Unity 6.3 WebGL built by GameCI, hosted on GitHub Pages at
https://jhiggins-tech.github.io/pooket-slug/, with online two-player co-op on Photon Fusion 2 and optional Google
sign-in for cloud saves. Mission one is solo and teaches the game; beating it opens the crew and co-op.

## Working rules
- **Branches** (the roster, agreed 16 Oct): the main line is `claude/gallant-keller-3qzp97` (`main` is only the
  first commit). Build each board item on `claude/<item-id>`, branched from the main line; a spike on
  `claude/spike-<name>`. Every push builds that branch's preview at
  https://jhiggins-tech.github.io/pooket-slug/previews/<branch without claude/>/ (the item's Try it link; all
  previews at `/previews/`). Only QA and release pushes the main line, once the owner approves the item on the
  Roadmap. Pull the main line before you start.
- **The Roadmap is the work queue** (https://claude.ai/artifact/1AWnQDGtUmiKMnjPgcbru9). An item going to review
  gets a page (a Claude Doc) and `page` and `line` on its card. The design is [docs/design](docs/design/README.md),
  the one source; its [history](docs/design/history.md) keeps the Roguelite and the ideas not picked.
- **Words players read**: "tabk", never tank, and "Trollogram", never hologram (code identifiers stay as they are).
  kie is said "k-eye".
- **Hidden ranks**: of the 33 ranks, only Bronze, Silver, Gold, Platinum, Diamond, Master, Grandmaster and Champion
  may be named. Never name or describe the other 25 outside `Assets/Scripts/Core/Lobby/Ranks.cs`: not in docs, tests,
  comments or commit messages.
- **Public repository**: never commit Photon SDK code (licensed; CI unpacks it from the private vendor repo),
  secrets or private-repo code.
- Spikes only answer "is it feasible"; they ride a `?spike=` flag and are never merged as they are. Rules start as
  tests against Core.

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
| `Core/` (assembly `PooketSlug.Core`, no references) | The rules, as plain C#, tested. At its root: `Content.cs` (soldiers, weapons, enemies, pickups as definitions with stable ids), `Definitions.cs`, `Tuning.cs` (shared numbers). Then, mirroring the game's folders: `Crew/` (`Crew.cs`: who you play; `<name>Tuning.cs` and specs, `Controls.cs`: the kit's meters, `KitGuide`, `TeamSuper`, `Lives`, `AvatarPose`), `Bosses/` (the boss tabks' specs, `PilotBrain`, `BossIntroShow`, `CraterGround`), `Tabks/` (`PlayerTankSpec`, `CallInCoach`), `Missions/` (`Level.cs` + `Missions.cs`: maps as text, `MissionStory`: a mission's scenes and once-only beats, `MissionBoard`, `CodaCoach`), `Story/` (`SceneScript` (the scene files' reader), `SceneLibrary` (which copy plays, and the check), `Staging`, `Cutscene.cs` (the player) + `Cues.cs`: the cue ids, `CrewReveal`), `Lobby/` (`Den`, `Debrief`: XP, `Ranks`, `Profile`: what's saved and reveals, `Options`, `InviteCode`), `Audio/` (`MusicTracks` / `Notation` / `Chip`: the chip music, its notation and the synth, `Wav`, `MusicMidi`), `Input/` (`ArcadePad`: the touch pad's logic, `SoldierInput`), `World/` (`TilePhysics`), `Platform/` (`LaunchFlags`: `?test=`, `?unlock=` and the rest, `Spike.cs`: `?spike=` flags, `PageQuery`, `Sandbox` + `Knobs` + `SetupText`, `NetContracts` / `NetIds` / `NetConfig`: co-op's interfaces and ids, `Rng`, `Log`) |
| `Game/` | `Game.cs`: `Bootstrap` and `Game`, the state machine (`GameState`: Title, Settings, Online, Lobby, Den, Cutscene, Playing, GameOver, Victory, Demo, Rooms), the world, the launch flags and the mission's story beats (from its `MissionStory`); then a flow per part of the game it hands frames to: `SceneFlow` (cutscenes, the site's copies, `?scene=` drafts), `DenFlow`, `KitFlow` (the ⓘ kit screen), `BoardFlow` (the mission screen), `DemoFlow` (`?demo=`), `SandboxFlow` (`?sandbox=`), `BossIntroFlow`, `MusicFlow`, `RoomsFlow` (the room browser), `DebriefFlow`, `RevealFlow` (two of the crew) |
| `World/` | One play-through (`World.cs`), rebuilt on restart, split by concern: `ScrollCamera`, `Spawner`, `Combat` (shots, hits, who owns damage), `Scoring`, `NetSync` (the world's side of co-op), `Hazards` (the fence, vents and icicles), `Barrel`, `Lift`, `Checkpoint`, `Liquids` (tones2's); and what every play-through is made of: `Entity` / `EntityPool`, `Projectile`, `Effect`, `Particle`, `Pickup`, `Prop`, `HitFeel`, `LevelView` |
| `Crew/` | `Shared/` (`Player`: your soldier, `RemotePlayer`: your partner's, `PlayerVisual`, `TeamSuperShow`), then one folder per crew member: `Tones2/` (kit, art, tabk guns), `Kie/` (kit, roulette), `Kcaj/` (kit) |
| `Enemies/` | `Enemies` (`Enemy` and the goons: `Rifleman`, `Runner`, `Drone`), `HoloLook` (Trollogram goons), `Weasels`, `Pilot` |
| `Bosses/` | `TankBoss` (the artillery boss tabk) + `BossKit` (a crew member's attacks on it: `KieBoss`, `KcajBoss`), `CraterView` (the diggable arena spike) |
| `Tabks/` | `PlayerTank` (your tabk) + `TankLook`, `TabkKit` (a crew member's tabk) |
| `Missions/One/` | Mission one's own pieces: the coda's `BackGate`, `ToppleTower` (the watchtower in the road), `ExitGate` and `SleepingKie`, drawn by `CodaArt` |
| `Stage/` | `StageKit`: the shared stage for cutscenes, the den and the debrief |
| `Art/`, `Audio/` | `Art` + `ArtData` (ASCII pixel art); `Sfx` + `ChipSounds`, `Music` |
| `Ui/` | uGUI built in code (`Ui.cs`): `HudView`, `MenuView`, `DenView` (the lobby), `CutsceneView`, `DebriefView`, `CrewRevealView`, `DemoClip` |
| `Input/` | Keyboard, gamepad and touch into one `SoldierInput` a frame |
| `Net/` (assembly `PooketSlug.Net`) | Photon Fusion 2, Shared Mode: `NetSession` (host, join, quick match), `NetReplication` (`PlayerNet`, `WorldNet`, `BossNet`), `NetPrefabs` (network objects built in code) |
| `Save/` | `LocalProfileStore` / `OptionsStore` (PlayerPrefs), `CloudAccount` (sign-in and cloud saves through the page's `pooket-auth.js`), `SiteFile` (a text file fetched from beside the game) |
| `Assets/Editor/WebGLBuild.cs` | The build: the scene, the WebGL template (`Assets/WebGLTemplates/PooketSlug`), the commit and secrets baked in |
| `Assets/Tests/EditMode/` | NUnit tests against Core, in the same folders as Core, run by CI before every build |
| `tools/` | `check.sh` (the local checks), `pages_deploy.sh` (the gh-pages deploy), `slug.sh` (a branch's preview folder), `vendor_fetch.sh` + `check_scenes.sh` (the vendor repo's sounds, the scene check), `coop.mjs` (co-op tests), `touch.mjs` (touch play), `demos.mjs` (kit demo clips), `unpack_unitypackage.py`, `new_meta.py` |
| `docs/design/` | The game design. `docs/research/`: how the pipeline, netcode, accounts and verification work, and [lessons learned](docs/research/lessons-learned.md) |

## How the game fits together
- **Flow**: `Game` owns the state and one `World`, and hands each frame to the flow for what's up (`Game/*Flow.cs`).
  Title (START, SETTINGS; CREW, MISSIONS and co-op appear as they're unlocked: progressive disclosure, `Profile`
  reveals) → cutscene → playing → the debrief, XP and rank.
- **Core and the rest**: rules live in Core with tests; entities and views draw, play sounds and call into Core.
  Core knows nothing of Unity's scene or the network.
- **Co-op** ([netcode](docs/research/netcode/README.md)): Shared Mode, so every client simulates its own soldier and
  the room's master client runs the shared world (enemies, the boss, pickups, scroll). The others report their hits.
  World talks to the network only through `INetLink`. Things share ids by rule (`NetIds`: a level spawn's id is its
  index). Only identical builds match, so each preview is its own co-op world.
- **Cutscenes** are text files in `scenes/` ([how to write them](docs/scenes.md)), read by `SceneScript` and staged by
  `CutsceneView`. `scene-files.yml` checks them and puts them up beside the game without a build; the game fetches
  them at start-up (its built-in copy is the fallback) and `?scene=<name>` plays a draft. The narrator's audio is made
  in the vendor repository (`voice/narration.json`) and fetched by CI; lengths come from the clips.
- **Launch flags**, read once as the game starts: `?spike=` (spikes, `Spike.cs`) and the rest into `LaunchFlags` (Core, tested): `?test=` (crate, tank, 1up, pilot,
  weak, team, kcajtank, boss, reveal, coda, debrief, crew, scene: jump straight to a part), `?unlock=` (coop, tank,
  crew), `?demo=` (a kit move's scripted demo; `all` records them for CI), `?room=` (an invite), `?log=` (log levels).
- **Sound and music**: Tabks' 8-bit recipes on the `Chip` synth, rendered at startup; the announcer, narration and
  orchestral music are WAVs CI fetches from vendor releases into `Resources` (`.bytes`, made into clips in code).

## Recipes
**Add a cutscene**: a file in `scenes/` ([docs/scenes.md](docs/scenes.md)) and its name in `SceneLibrary`, cues from
`Cues` (`CueTests` holds every cue to having an effect), narrator lines in the vendor repo's `voice/narration.json`,
then where it plays in `Game`.

**Add a weapon**: its `WeaponDef` (and `ProjectileDef`, if it fires one) in `Content`, with the next unused id
(ids ride the network: never change or reuse one), its sprite in `Art` / `ArtData` (keyed by the projectile's id),
its sound in `Sfx` / `ChipSounds`, and the kit that fires it.

**Add a sound cue**: in a scene, its constant in `Cues` and what it does in `CutsceneView`; in the boss intro, its
place on the `Cue` timeline in `BossIntroShow`. Either way its sound goes in `Sfx` / `ChipSounds`.

**Add a mission**: a `MissionDef` in `Missions` (map rows: `Level.cs` says what each character is), its story
(`MissionStory`: its scenes, coda, solo only), its spawns, and where it opens (`Profile`, `MissionBoard`).

**Add a crew member**: a `SoldierDef` in `Content` (stable id), their numbers in `Core/Crew/<Name>Tuning.cs` with tests,
their kit in `Crew/<Name>/`, art in `ArtData`, sounds in `Sfx` / `ChipSounds`, their place in `Crew`, their kit
screen demos (`KitGuide`), team supers (`TeamSuper`), and their boss tabk if they have one (`BossKit`).

**Add a spike**: a key in `Spike.cs` (solo only unless it says otherwise), on branch `claude/spike-<name>`. Its
preview link with `?spike=<key>` is the Try it link.

**Change what crosses the network**: the structs in `Net/` and `Core/Platform/NetContracts.cs`; content ids never change or
get reused (the tests pin them).

## Invariants and gotchas
- Gameplay randomness only through a seeded `Rng`; cosmetic effects use their own.
- The build is uncompressed: Pages gzips it on the fly ([why](docs/research/build-pipeline/github-pages.md)).
- Docs-only pushes (`**.md`, `docs/**`) don't build: a redeploy changes the build version co-op players must share.
- Fusion's own config (`NetworkProjectConfig.fusion`) is committed and wins over the one the SDK ships.
