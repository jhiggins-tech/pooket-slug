# Refactor plan (proposal)

What the code, build and netcode need before the owner's chosen features can be built, and in what order. It's based on the owner's MoSCoW pass over [feature-candidates.csv](feature-candidates.csv) and on the game design in [DESIGN.md](../../../DESIGN.md).

**Status, 2026-10-14:** done. All 16 steps landed in small commits, each checked locally and in CI ([test log](../verification/test-log.md#part-4-refactor)). Parts that need a feature first wait for it; each step says which. The owner answered its [decisions](#decisions-2026-10-14), and the game design it serves is signed off in [docs/design](../../design/README.md).

**Read this when** you're approving the refactor, or about to start one of its steps.

Up: [Roadmap](README.md)

## What the chosen features ask of the code

The Must and Should rows, plus the design, fall into a few groups. Each one presses on a weak spot in today's code ([refactor candidates](refactor-candidates.md)):

| The game needs | Rows | Where today's code falls short |
|---|---|---|
| Eight soldiers, each with a primary, a special, a super and sometimes an ability. Weapon levels with a fork at II, status effects, and per-soldier movement | CHR-02/03/05, MOV-01, AIM-06, WPN-02…10, WPN-12, TRN-01 | Weapons and enemies are `switch` statements over enums, spread over four files |
| Things that appear mid-game: drops, paratroopers, truck troops, kie's weasels and decoys, kcaj's 120 pills, a called-in tank | WPN-13, ENM-05/06, the kits, VEH-01 | Only things in the level's spawn list have a network identity |
| Hordes, multi-stage bosses, tanks to ride | ENM-03…08, BOS-01/02/05, VEH-01/02/06 | The shared world holds at most 24 enemies and 64 pickups, in fixed tables |
| Hearts, lives and 1-ups for each player; per-player score and stats; kits that help a partner | SCR-03, SCR-06/07, COP-02, COP-08 | Per-player network state is only position, pose, health and skin, and one wide interface carries every network event |
| A lobby with ready-up, a room browser, spectators, invites, a fixed Australian region, reconnects | NET-01…07, NET-09, SOC-04 | `NetSession` mixes connecting, sessions and replication, and a room holds exactly 2 |
| A touch arcade pad, phone performance, an installable web app | ACC-02, ACC-04, ACC-07, ACC-08 | Input is one keyboard-and-gamepad snapshot, and the menus and HUD are IMGUI |
| Sign-in, cloud saves, names, unlocks, cosmetics, stats | SOC-01…03, SCR-07, SCR-09, CHR-04 | The only save is one high-score key |
| Missions as content | LVL-01/02, LVL-06…08, PRE-06, AUD-02 | One hard-coded map |
| Releases and tests | OPS-04, TEC-08, TEC-09 | Every push deploys live, and nothing is tested automatically |

## The plan

The work comes in four phases, each done in small commits. Each phase ends with a playable build that plays no worse than before. Numbers in brackets are [refactor candidates](refactor-candidates.md).

**Phase 1: safety net and cheap wins** (each step is small)
1. **Housekeeping:** logging with categories (#16), tuning values in one place (#12), and seeded randomness (#17, which TEC-09 replays need). **Done** 2026-10-14.
2. **Tests:** pure classes, with EditMode tests, for what's about to move: tile physics, level parsing, snapshot packing and room codes (#15, TEC-08). Add a test step to CI, which adds a few minutes to each build. **Done** 2026-10-14: the `PooketSlug.Core` assembly, with [EditMode tests](../verification/edit-mode-tests.md) that run before every build. The step adds about a minute.
3. **Pooling** for projectiles, particles and popups (#10, TEC-01). kcaj's Unmedicated alone is 120 pills. **Done** 2026-10-14 for projectiles, explosions and debris. Popups were already plain structs, so they need no pool.
4. **A two-player test in CI** (OPS-02, promoted by the owner): after each deploy, run the two-browser co-op test against it. That's needed before phase 3. **Done** 2026-10-14: it passed on its first run ([test log](../verification/test-log.md#part-2-photon-fusion-co-op)).

**Phase 2: structure** (each step is medium)

5. **Split `World` into systems:** spawning, combat and damage (with statuses, TRN-01), camera, network sync, and scoring with the tally (#1, TEC-03). **Done** 2026-10-14: `ScrollCamera`, `Spawner`, `Combat`, `Scoring` and `NetSync` in [`World/`](../../../Assets/Scripts/World/). Statuses and the tally come with their features.
6. **Content as data.** Soldiers and their kits, weapons built from behaviours (bullet, lob, beam, cone, homing, spray; levels I–III with a fork at II), statuses, enemies, crates and tanks. Each gets a stable ID that network messages use (#2, #12, TEC-02). The classic Metal Slug gun styles are kit behaviours, not separate guns (the owner's call). **Done** 2026-10-14 for today's content: projectiles, weapons, enemies, pickups and the soldier are definitions in [`Content`](../../../Assets/Scripts/Core/Content.cs), with byte ids that shots and snapshots send. New behaviours, levels, statuses and tanks are added as definitions when they're built.
7. **Typed network events,** with smaller interfaces in place of the single `INetLink` (#6). **Done** 2026-10-14: `INetEvents` with typed payloads, and `INetLink` made of `ISessionView`, `ISoldierChannel` and `IWorldChannel` ([architecture](../netcode/architecture.md#the-boundary-between-game-and-netcode)).
8. **Assembly definitions** (#5, TEC-04; agreed): core and netcode assemblies, done while the files move. **Done** 2026-10-14: `PooketSlug.Net` joins `PooketSlug.Core`. Nothing references it, so the compiler keeps gameplay away from Fusion.

**Phase 3: netcode scale** (each step is medium, with a two-player test after each)

9. **Dynamic network IDs** (#3, TEC-05). **Done** 2026-10-14: [`NetIds`](../../../Assets/Scripts/Core/NetIds.cs). Level spawns keep their index, and anything spawned mid-game counts up from a counter in the shared world, which a new authority continues.
10. **Bigger replicated tables** for many small things, and a network object each for bosses and tanks (#4, TEC-06). **Done** 2026-10-14 for pickups: they're a table of live ones, like enemies, so a mission can have any number, and drops work. The enemy table stays at 24 live. The boss got its network object (`BossNet`: its turret, charge and shots) on 2026-10-15, when the artillery boss tank became Mission one's boss. Rideable tanks get theirs when that feature arrives.
11. **Per-player state:** soldier, kit levels, hearts, lives and statuses (#7, for two players only, since COP-04 is Won't). **Done** 2026-10-14 for the soldier: each player's `SoldierDef` is replicated, and a partner is drawn and counted with their own soldier's size and health. Kit levels, lives and statuses join `PlayerNet` with their features.
12. **Split `NetSession`** in two:
    - connecting and sessions: the fixed AU region, the room browser, the lobby with ready-up, spectators, and reconnecting (NET-02…05, NET-09)
    - replication.

    With the region fixed, room codes can drop their region suffix and be just four letters.

    **Done** 2026-10-14 as a split: `NetSession` connects and `NetReplication` shares. The lobby features (room browser, ready-up, spectators, reconnecting) build on it later. The AU region and four-letter codes followed as [C3b](../todo.md), on 2026-10-14.

**Phase 4: front end and platform** (each step is medium)

13. **Input sources,** including the touch arcade pad (#8, ACC-02). **Done** 2026-10-14: keyboard, gamepad and touch sources add up into one `SoldierInput`, and the [arcade pad](../build-pipeline/unity-project.md#input) follows the design. Input System actions weren't needed: rebinding (UIX-03) and couch co-op (COP-05) are Won't.
14. **Game flow and uGUI, built in code** (#9; UIX-01, now a Must): the mission screen, character select, lobby, room browser, settings and HUD. Menus and HUD grow with the player's unlocks and reveals ([progressive disclosure](../../design/README.md#progressive-disclosure-the-thread-through-all-four)), and an invite link opened before co-op is unlocked starts Mission one instead. **Done** 2026-10-14 in two parts: the HUD (14a), then the menus and the flow (14b), with IMGUI gone. Co-op appears once Mission one is beaten solo, and invite links respect that. Character select waits for a second soldier, as the design asks; the room browser and lobby wait for step 12's features; settings (UIX-02) aren't started. uGUI added about 3.7 MB to the download (36%), mostly UI Toolkit that its event system reaches; stripping can't remove it ([numbers](../build-pipeline/numbers.md#what-ugui-costs)).
15. **A profile and save interface,** local and cloud (#13, SOC-02, TEC-07). This joins the [sign-in work](../accounts/README.md). **Done** 2026-10-14 for local: [`Profile`](../../../Assets/Scripts/Core/Profile.cs) behind `IProfileStore`, with a merge for the cloud copy. It came before 14b, which needs it. The cloud store waits for sign-in ([C1](../todo.md)).
16. **Levels as data,** with a mission list (#11, LVL-01/02). **Done** 2026-10-14: [`Missions`](../../../Assets/Scripts/Core/Missions.cs) lists each mission's id, net id, name and map, and the shared world says which mission it is. The maps stay ASCII in code; LDtk or JSON can come with real level work (LVL-03).

**Build and CI, alongside the phases**
- **Releases:** branch pushes keep deploying the live game, as now (the owner's call). Tagged versions and a changelog (OPS-04) can come later without changing that.
- **The rest:**
  - the test step (TEC-08), done with step 2
  - the sign-in settings ([C1](../todo.md))
  - the region fixed to AU (NET-05)
  - a web app manifest in the page template (ACC-07)

**Not planned**, because each is marked Could or Won't:
- more than two players (COP-04)
- branch previews (OPS-01)
- pinned actions (OPS-05)
- deterministic simulation (TEC-10)
- a level editor (LVL-03)
- URP (PRE-05)
- cheat resistance (NET-08)

**Checks for every step:** a local type-check and weave, a green CI build, and a smoke test of the deploy. Netcode steps also need the CI two-player test (step 4).

## Your questions

**TEC-05, dynamic network IDs.**
- **What it is:** today both phones know what "enemy 7" means, because it's entry 7 in the level's spawn list, which both phones have. Something that appears mid-game has no entry, so one phone can't tell the other about it. That covers a random drop, a paratrooper, a weasel or a called-in tank. With dynamic IDs, the phone running the world hands out a fresh number for each new thing and tells the other phone.
- **Verdict:** needed for WPN-13 (Must), most of the kits, and ENM-05/06 (Should). Treat it as a Must.

**TEC-06, scalable replicated entities.**
- **What it is:** the shared world is a fixed-size table, with room for 24 live enemies and 64 pickups per level. Hordes of goons, drops and kie's Trollogram copies will run out of room. The fix is bigger or paged tables for the many small things, and a network object each for big things, such as boss tanks and the tanks you ride.
- **Verdict:** needed once Mission one has hordes. Treat it as a Must, done in steps.

**UIX-01, a real UI system: the trade-offs.**

| | IMGUI (today) | uGUI | UI Toolkit |
|---|---|---|---|
| Touch | One touch, treated as a mouse | Multitouch, through its event system | Multitouch, through its own events |
| The arcade pad | Touch handling written by hand | The Input System's ready-made on-screen stick and buttons | Controls written by hand |
| Phone screens | Scaling done by hand | Canvas scaling built in | Flexbox layout and stylesheets |
| Building it in code | Natural | Wordy but fine | Fine; stylesheets are optional |
| Drawbacks | Redraws every frame; glyphs go missing | Draw calls to watch | Fewer ready-made game controls |

**Recommendation: uGUI, built in code. Promote UIX-01 to Must.** *Accepted by the owner, 2026-10-14.*
- **Why:** the arcade pad (ACC-02, Must) needs multitouch, and the Must list also adds a lobby, a room browser, character select and cosmetics screens.
- **IMGUI** stays for debug overlays.

**TEC-04, assembly definitions: the trade-offs.**
- **For:**
  - The compiler makes sure gameplay never touches Photon.
  - Building without the SDK becomes a setting on the netcode assembly, instead of `#if` blocks.
  - The weaver only touches netcode.
- **Against:**
  - The netcode assembly has to be added to Fusion's weave list, which is a committed setting.
  - The local verification tools assume one assembly, so they'd need updating.
- **Recommendation: Could is right.** It's cheapest during the `World` split, when the same files move anyway. *Accepted by the owner, 2026-10-14: step 8.*

## How the "See design doc" rows read

| Row | My reading of DESIGN.md | In scope |
|---|---|---|
| VEH-01 Tank vehicle | Ride the crew tanks you've won | Yes |
| VEH-02 Enter, exit, eject, ram | Climb in; eject, and the empty tank rolls on into the goons | Yes |
| VEH-06 Hijack enemy vehicles | Beat a crew tank and its pilot, then climb in | Crew tanks only |
| ENM-01 Starter enemy set | The spike's three goons, as holograms | Yes |
| ENM-02 Bazooka and mortar soldiers | Not in Mission one | Later missions |
| ENM-10 Alien faction | The goons are holograms; there's no second faction | No |
| ENM-12 Waves and arena locks | Boss arenas only | Arenas |
| ENM-13 Scaling by player count | Missions don't scale; the late missions and the hard setting are tuned for two | No scaling |
| BOS-01 Tank boss | Each mission ends with a crew tank, kie's first | Yes |
| BOS-02 Multi-phase bosses | Faster as it's hurt, then the pilot round | Yes |
| BOS-03, BOS-04 Multi-part, screen-filling | Not in the design | No |
| POW-02, POW-03 Prisoners and their tally | Freeing a crewmate is the rescue | No |
| POW-04 Food, treasure and medals | Not in the design | No |
| POW-06 Helper characters | Kit helpers only: torikloud's twin, larinovsky's cats | As kit moves |
| POW-07 Sharing items in co-op | A 1-up goes to a partner who's out; Band Aid heals partners | Partly |
| TRN-01 Status effects | Burn, Cooked, Tattooed, Pinned, Toxic | Yes |
| TRN-02 Transformations | Not in the design | No |
| SCR-01 Score and high score | The score on the HUD | Yes (exists) |
| SCR-02 Mission tally and rank | The tally and the rank ladder | Yes, as the design has them |
| SCR-03 Lives and continues | Each player has their own | Yes |
| SCR-04 Difficulty levels | A hard setting for every cleared mission, tuned for two players | Yes |
| SCR-05 Combo multiplier | Not in the design | No |
| CHR-05 Special moves | Special, super and ability | Yes |
| COP-02 Revive a downed partner | A 1-up goes first to a partner who's out | Yes, by 1-up |
| COP-03 Team lives | Not picked | No |

Other entries, as I read them:
- **MOV-01 and AIM-06** ("character point of differences") vary by soldier, so they belong in each soldier's data.
- **WPN-01** ("part of base kit") is covered by each soldier's primary and special.
- **LVL-14 and CHR-01's customisation** are phase 2.
- **TEC-07** (Won't) is covered anyway by cloud saves (SOC-02, Must), which need the same interface.

The owner's answers on the [design documents](../../design/README.md) (2026-10-14) also settle:
- **LVL-04** (Could): in after all, as a checkpoint at each boss door.
- **AIM-04** (Should): melee is a kit trait for some soldiers, not everyone's.
- **COP-08** (Must): team moves are the kits that help a partner, plus a team super when both supers are charged.

## Decisions (2026-10-14)

The owner's answers:

| Question | Decision |
|---|---|
| Leaderboards (SCR-10) | **A friends board** for each mission, visible to signed-in players. It runs on trust |
| Releases (OPS-04) | **Branches keep deploying** the live game, as now |
| Two-player tests | **Automated in CI:** OPS-02 is promoted (step 4) |
| Classic guns (WPN-02…10) | **Covered by the kits:** there are no separate classic guns |
| UI system and assembly definitions | **Both accepted:** uGUI built in code (step 14) and assembly definitions (step 8) |

## The player record, scoped by the MoSCoW pass

The sign-in work extends its [drafted record](../accounts/database-rules.md) before its first Cardmaster pull request, to what the Must rows and the design keep per player:

| Field | What it holds | Rows |
|---|---|---|
| `name` | Display name, chosen by the player | SOC-03 |
| `crew` | Freed crewmates | SCR-09 |
| `coop` | Whether co-op is open (Mission one beaten solo) | SCR-09 |
| `xp` | Career XP; it never goes down | CHR-04 |
| `tokens` | Tokens from XP levels, spent on looks | CHR-04 |
| `cosmetics` | Items owned, and what's worn in each slot | CHR-04 |
| `rank` | Stages beaten; it never goes down | Ranks |
| `tally` | Per soldier and weapon: shots, hits, damage dealt and taken, kills | SCR-07 |
| `best`, `clears` | Best score, and clears per mission and setting (normal or hard) | SCR-01, SCR-04, LVL-02 |
| `seen` | Reveals already shown | Progressive disclosure |
| `first`, `ts` | First played, and last saved (server time) | Tracking |

- **`clears` replace the draft's `runs` and `wins`.**
- **`seen`** lists the reveals already shown, such as the first tank or the first drive. Each reveal then plays once, and the menus know what to show ([progressive disclosure](../../design/README.md#progressive-disclosure-the-thread-through-all-four)).
- **The friends leaderboard** is a separate list per mission: each player's display name and best score, readable by any signed-in player and written only by its owner.
- **Committed 2026-10-14** to a Cardmaster branch, with one addition: a save number (`rev`) that every save raises by one, so a device that's behind can't overwrite newer progress ([database rules](../accounts/database-rules.md)).
