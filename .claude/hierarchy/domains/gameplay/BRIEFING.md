# The gameplay domain

## What this domain is
The rules of play: the crew's kits and tuning, the missions and their maps, the goons, the bosses and their pilots,
the tabks, the world a play-through runs in, the content table and its ids, the input that drives a soldier, and the
platform's small half (launch flags, spikes, the sandbox, the seeded random source). Rules live in Core with tests; the
entities and views outside Core draw them, play sounds and call into Core.

## The files
The list is `.claude/hierarchy/OWNERSHIP.txt` (the map is the authority; this is a copy, and it has to match).
Paths ending in `/` mean everything under them.

- `Assets/Scripts/Core/Content.cs`
- `Assets/Scripts/Core/Definitions.cs`
- `Assets/Scripts/Core/Tuning.cs`
- `Assets/Scripts/Core/Palette.cs`
- `Assets/Scripts/Core/Crew/Crew.cs`
- `Assets/Scripts/Core/Crew/AvatarPose.cs`
- `Assets/Scripts/Core/Crew/Controls.cs`
- `Assets/Scripts/Core/Crew/DownLine.cs`
- `Assets/Scripts/Core/Crew/Drip.cs`
- `Assets/Scripts/Core/Crew/Lives.cs`
- `Assets/Scripts/Core/Crew/SoldierJump.cs`
- `Assets/Scripts/Core/Crew/TeamSuper.cs`
- `Assets/Scripts/Core/Crew/KitGuide.cs`
- `Assets/Scripts/Core/Crew/KcajTabkSpec.cs`
- `Assets/Scripts/Core/Crew/KcajTuning.cs`
- `Assets/Scripts/Core/Crew/KieTabkSpec.cs`
- `Assets/Scripts/Core/Crew/KieTuning.cs`
- `Assets/Scripts/Core/Crew/Tones2TabkSpec.cs`
- `Assets/Scripts/Core/Crew/Tones2Tuning.cs`
- `Assets/Scripts/Core/Bosses/BossTankSpec.cs`
- `Assets/Scripts/Core/Bosses/CraterGround.cs`
- `Assets/Scripts/Core/Bosses/KcajTankSpec.cs`
- `Assets/Scripts/Core/Bosses/KieTankSpec.cs`
- `Assets/Scripts/Core/Bosses/PilotBrain.cs`
- `Assets/Scripts/Core/Tabks/`
- `Assets/Scripts/Core/Missions/Missions.cs`
- `Assets/Scripts/Core/Missions/Level.cs`
- `Assets/Scripts/Core/Missions/MissionBoard.cs`
- `Assets/Scripts/Core/Missions/StartFlow.cs`
- `Assets/Scripts/Core/Missions/CodaCoach.cs`
- `Assets/Scripts/Core/Missions/Creamery.cs`
- `Assets/Scripts/Core/Enemies/`
- `Assets/Scripts/Core/World/`
- `Assets/Scripts/Core/Input/`
- `Assets/Scripts/Core/Platform/LaunchFlags.cs`
- `Assets/Scripts/Core/Platform/Spike.cs`
- `Assets/Scripts/Core/Platform/PageQuery.cs`
- `Assets/Scripts/Core/Platform/Sandbox.cs`
- `Assets/Scripts/Core/Platform/Knobs.cs`
- `Assets/Scripts/Core/Platform/SetupText.cs`
- `Assets/Scripts/Core/Platform/Rng.cs`
- `Assets/Scripts/Core/Platform/Log.cs`
- `Assets/Scripts/World/BackdropView.cs`
- `Assets/Scripts/World/Barrel.cs`
- `Assets/Scripts/World/Checkpoint.cs`
- `Assets/Scripts/World/Combat.cs`
- `Assets/Scripts/World/Effect.cs`
- `Assets/Scripts/World/Entity.cs`
- `Assets/Scripts/World/EntityPool.cs`
- `Assets/Scripts/World/FrostLook.cs`
- `Assets/Scripts/World/Hazards.cs`
- `Assets/Scripts/World/HitFeel.cs`
- `Assets/Scripts/World/LevelView.cs`
- `Assets/Scripts/World/Lift.cs`
- `Assets/Scripts/World/Liquids.cs`
- `Assets/Scripts/World/Particle.cs`
- `Assets/Scripts/World/Pickup.cs`
- `Assets/Scripts/World/Projectile.cs`
- `Assets/Scripts/World/Prop.cs`
- `Assets/Scripts/World/Scoring.cs`
- `Assets/Scripts/World/ScrollCamera.cs`
- `Assets/Scripts/World/SetPiece.cs`
- `Assets/Scripts/World/Spawner.cs`
- `Assets/Scripts/World/World.cs`
- `Assets/Scripts/Crew/`
- `Assets/Scripts/Enemies/`
- `Assets/Scripts/Bosses/`
- `Assets/Scripts/Tabks/`
- `Assets/Scripts/Missions/`
- `Assets/Tests/EditMode/Bosses/BossTankTests.cs`
- `Assets/Tests/EditMode/Bosses/CraterGroundTests.cs`
- `Assets/Tests/EditMode/Bosses/KcajTankTests.cs`
- `Assets/Tests/EditMode/Bosses/KieTankTests.cs`
- `Assets/Tests/EditMode/Bosses/PilotBrainTests.cs`
- `Assets/Tests/EditMode/Crew/`
- `Assets/Tests/EditMode/Enemies/`
- `Assets/Tests/EditMode/Input/`
- `Assets/Tests/EditMode/Missions/CodaTests.cs`
- `Assets/Tests/EditMode/Missions/CreameryTests.cs`
- `Assets/Tests/EditMode/Missions/LevelTests.cs`
- `Assets/Tests/EditMode/Missions/MissionBoardTests.cs`
- `Assets/Tests/EditMode/Missions/MissionsTests.cs`
- `Assets/Tests/EditMode/Missions/StartFlowTests.cs`
- `Assets/Tests/EditMode/Tabks/`
- `Assets/Tests/EditMode/World/`
- `Assets/Tests/EditMode/ContentTests.cs`
- `Assets/Tests/EditMode/Platform/LaunchFlagsTests.cs`
- `Assets/Tests/EditMode/Platform/LogTests.cs`
- `Assets/Tests/EditMode/Platform/PageQueryTests.cs`
- `Assets/Tests/EditMode/Platform/RngTests.cs`
- `Assets/Tests/EditMode/Platform/SandboxTests.cs`
- `Assets/Tests/EditMode/Platform/SpikeTests.cs`
- `Assets/Tests/Gameplay/BossTests.cs`
- `Assets/Tests/Gameplay/CombatTests.cs`
- `Assets/Tests/Gameplay/GameplayFixture.cs`
- `Assets/Tests/Gameplay/SetPieceTests.cs`
- `Assets/Tests/Gameplay/SpawnerTests.cs`
- `Assets/Tests/Gameplay/WorldTests.cs`
- `sandbox/`

Not this domain's, though they sit beside its files: `Core/Missions/MissionStory.cs` (story), `Core/Audio/` (audio),
`Core/Anim/` (`Clip`, `Fx`, `StagePose`: sprites), `World/NetSync.cs` and `Tests/Gameplay/NetSyncTests.cs` (net),
`Core/Bosses/BossIntroShow.cs` (cutscene), `Core/Lobby/` and `Ui/Den/` (den), the rest of `Core/Platform/` (net and shell).
`docs/design/` pages are claimed by other domains in the map; the ones this domain decides are in the next section.

## The rules this domain keeps
- Rules start as tests against Core. A rule living in a view, a flow or an entity moves into Core, with tests, the next
  time that code changes. (`CLAUDE.md`, "Leave it better")
- Gameplay randomness goes only through a seeded `Rng` (`Core/Platform/Rng.cs`); cosmetic effects use their own. The
  hazards run off `World.Elapsed` so every phone agrees without the network (`HazardBeat`, `Creamery`, `BaseHazards`).
- Never scale enemies, bosses or pilots for co-op: the missions do not change with two players. (`docs/design/levels.md`,
  Difficulty)
- There is no hard setting: each mission is harder than the one before it, and that climb is the hard mode (owner,
  25 Oct, card dca-hard-pick). `MissionBoard` has no `HardOpen` or `SettingFor`, a `MissionCard` holds one `Best` and
  `Clears`, and `Profile.RecordRun`, `Best` and `Clears` take no setting. Do not bring back a difficulty pick without the
  owner. (`levels.md`, Difficulty; `MissionBoardTests`)
- Hit and goon-only logic runs on the authority (the room's master client); a hazard hurts only this phone's soldier and
  plays its sounds only on screen. (`CLAUDE.md`, Add a hazard)
- A goon is a Trollogram unless its `EnemyDef` says `real: true`; only set pieces, the barrel, the weasel and the boss
  tabk are real. (`ContentTests.OnlyTheseAreRealEverythingElseIsATrollogram`)
- The two kinds of tabk are separate entities: the boss tabk (artillery: `BossTankSpec`, `Bosses/TankBoss.cs`) and your
  driven or called-in tabk (`PlayerTankSpec`, `Tabks/PlayerTank.cs`). Your tabk arrives with full fuel and never
  refuels (dry, it cannot drive or hop but still fires; eject to leave), and its rounds never refill.
  (`docs/design/tanks.md`, `PlayerTankSpec`)
- Every team super is a short show, then every enemy on screen dies and a boss takes a big hit; solo supers stay varied
  and are not screen clearers. A combo exists only when both its soldiers are freed and it is built: no combo, no TEAM
  button. (`docs/design/team-supers.md`, `TeamSuperTests`)
- Who is freed: tones2 from the start, kie by clearing Mission one solo, kcaj by clearing Mission two. Nothing about it
  is saved; it comes from the mission records. Two partners cannot play the same soldier or load the same tabk.
  (`Crew.cs`, `CrewTests`, `CoopTabks.cs`, `CoopTabksTests`)
- Mission one is solo only and opens co-op when beaten; a co-op room never plays a solo mission. (`Missions.cs`,
  `MissionsTests.ACoopRoomNeverPlaysASoloMission`)
- A boss tabk is wrecked, then its Trollogram pilot jumps out and fights on foot with the crewmate's own kit. The pilot
  sees only what is on screen and never reads a player's input. (`PilotBrain`, `docs/design/tabks/bosses.md`)
- No mission hands out a classic gun; every mission's pickups fit the shared pickup table. (`MissionsTests`)
- A new `.cs` file goes in the folder for what it is about and needs a `.meta` (`tools/new_meta.py`); each file starts
  with a summary comment.

## The vocabulary
- Words players read: "tabk", never tank; "Trollogram", never hologram. Code identifiers keep their old names
  (`PlayerTank`, `TankBoss`, `HoloLook`, `BossTankSpec`). kie is said "k-eye".
- Players are told only of Bronze through Champion among the ranks; never name the other 25 in text players read.
- The soldiers are `Content.Trooper` (0), `Tones2` (1), `Kie` (2), `Kcaj` (3). The crewmates are `Crew.Tones2`, `Kie`,
  `Kcaj`; a kit is a `CrewKit` (`Crew/Shared/CrewKit.cs`), a partner's is a `*Copy` (`KitCopy.For`).
- Missions are `Missions.One` (`mission-1`, net id 0) and `Missions.Two` (`mission-2`, net id 1). A map is text rows
  read by `Level`; `Level.Markers` says what each character places (a `SpawnKind`).
- Team combos are `TeamCombo` (`SewerRats` 1, `MentosAndCola` 2, `PillWeasels` 3). A move a partner copies is a
  `KitMove`; a shot is a weapon id.
- `?test=`, `?unlock=`, `?demo=`, `?mission=` are `LaunchFlags`; `?spike=` is `Spike.cs`; `?sandbox=` is `Sandbox`, with
  `Knobs` for tuning from a link and the files in `sandbox/`.

## The invariants
- **Content ids ride the network and are never changed or reused.** `ContentTests` pins every weapon (0-27, retired
  18 and 19 too), projectile (0-15), enemy (0-17), pickup (0-3) and soldier (0-3) by number to its named definition,
  and each table's size, so a new definition fails until it gets its pin (pin-net-ids card, 25 Oct). `GarageTests`
  pins the tabk cards' ids (their owners' soldier ids, sent as the loaded tabk); `MissionsTests` the missions' net ids;
  `TeamSuperTests` the combos; `CrewKitTests` the `KitMove`s. A retired definition stays in the table (the old tabk
  shell and mortar are projectile ids 9 and 10). Add the next unused id.
- Each table holds its definitions at their own index (`ContentTests.EachTableHoldsItsDefinitionsAtTheirIds`).
- A level spawn's network id is its index in the map; things spawned mid-game get ids after the levels
  (`SpawnerTests.ALevelSpawnKeepsItsIndexAsItsId`, `ThingsSpawnedMidGameGetIdsAfterTheLevels`).
- Every map marker places something and every brain has a class to play it (`SpawnerTests`); a map character used twice
  throws when `Level.Markers` is built.
- Every mission starts on the ground, ends at the flag and has one boss (`MissionsTests`).
- The play numbers in `Tuning` are fields, not constants, so the sandbox can tune them (`Knobs`); the camera, co-op and
  HUD ones are built in.
- A blink, flicker or walk cycle goes through `Fx` or a `Clip`, not an inline modulo of time; a figure on a stage is
  posed with `StageFigure.Pose`. (`CLAUDE.md`)
- Spikes only answer "is it feasible", ride a `?spike=` flag and are never merged as they are. Only `craters` is left
  in `Spike.cs`; the others became real features (`SpikeTests`).
- Do not commit Photon SDK code, secrets or private-repo code. World talks to the network only through `INetLink`; a
  change to what crosses the network is the net domain's, and `World/NetSync.cs` is not ours.
- The recipes (add a weapon, a goon, a hazard, a mission, a crew member) are in `CLAUDE.md`; follow them, they name every
  place a new piece must be registered.

## How it is checked
`tools/check.sh` (type-check as for WebGL, Fusion's weaver, the EditMode tests, the scene check, the docs link check);
`tools/check.sh compile` for a type change. There is no Unity licence in a cloud session, so CI runs the PlayMode
gameplay tests (`Assets/Tests/Gameplay/`: `World` ticked frame by frame through `GameplayFixture`, for combat, bosses,
the spawner and set pieces) and the WebGL build with touch play. The EditMode tests are in the folders that mirror Core
(`Crew/`, `Bosses/`, `Tabks/`, `Missions/`, `Enemies/`, `World/`, `Input/`, `Platform/`, and `ContentTests.cs`). A
change to the netcode also needs the co-op tests (`CI: co-op` in a commit message forces them).

## Where the docs moved (stale-docs card, 25 Oct)
- `docs/design/tanks.md` is an index now; the detail is in `docs/design/tabks/` (`bosses.md`: the boss tabks and
  the pilot round; `driving.md`: driving and calling in; `tones2.md`, `kie.md`, `kcaj.md`: each crewmate's tabk). The
  boss intro's design is the cutscene script, `docs/design/cutscenes/mission-one.md`. Point comments there, not at
  tanks.md's old sections. A few comments still name tanks.md for what is still on it (`Garage`, `TabkKit`,
  `BossTankSpec`'s "Two kinds of tabk").
- The title has only START and SETTINGS: the crew pick, the mission board and co-op are all in the den. A comment
  naming a title button (MISSIONS, CREW, CO-OP) is stale. Drop-in and its own scores are built (`DropIn`), no longer a
  spike; `Spike.cs` holds only `craters`, so any other `?spike=` in a comment is stale.
- `Missions.ForCoop` means a co-op room never plays Mission one; `MissionBoard.StartBanner` still handles the case.

## Known unfinished
- Mission order after kcaj, and each later mission's setting, is an open question (`docs/design/levels.md`). Only two
  missions exist.
- Each tabk's boss attacks after kcaj's are not worked out; shotdownboyz, lankcity, odsey and doctorfox have no kit,
  mission or tabk. garyoldmancorp's real moves are undesigned. (`kits.md`, `tanks.md`)
- Team combos: three are built; the rest wait for their pair to exist. The team-supers page is still marked draft and
  names and gags are first ideas. (`docs/design/team-supers.md`)
- To tune in play: the call-in's charge-up time, whether your tabk jumps, kit timings (ten-1's build-up, ten-3's spew,
  Sew's pin, the scam window), career XP amounts, how far the late missions go past one player, melee for which soldiers.
- Earning call-ins and garage upgrades was asked of the Roadmap manager as an idea card, after Mission two. Unknown
  whether it was queued; check the Roadmap (https://claude.ai/artifact/1AWnQDGtUmiKMnjPgcbru9) before building it.
- The craters spike (`Core/Bosses/CraterGround.cs`, `Bosses/CraterView.cs`) is still behind `?spike=craters`. The design says craters can be dropped
  and arenas made solid if they prove very hard (`levels.md`); no decision is recorded.
- `Missions/Two/CreameryArt.cs` says its pieces are code-drawn stand-ins until Design's art lands. Not checked
  whether any of that art has landed; if it has, the comment is stale.
