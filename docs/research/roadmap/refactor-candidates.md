# Refactor candidates (draft)

The structural improvements we'd make with hindsight, now that the spike works. The [refactor plan](refactor-plan.md) picks and orders these against the MoSCoW results in [`feature-candidates.csv`](feature-candidates.csv). Each one lists the feature IDs it unlocks, so the choice can follow the roadmap.

**Read this when** you're planning the refactor pass, or about to add a feature that fights the current structure.

Up: [Roadmap](README.md)

## Where the code is today

- **Size:** about 4,100 lines of C# in `Assets/Scripts`, one assembly (`Assembly-CSharp`), plus a 140-line build script.
- **The big files:** `World.cs` (583 lines), `Game.cs` (518), `NetSession.cs` (467), `Art.cs` + `ArtData.cs` (775, procedural art), `Enemies.cs` (398).
- **The design held up well for a spike:**
  - plain C# entities driven by one `World`
  - a small interface between game and netcode
  - everything built in code

  The pressure points below are where new features will hurt.

## Candidates

| # | Refactor | Why, with hindsight | Unlocks | Size |
|---|---|---|---|---|
| 1 | **Split `World` into systems:** spawning, combat (collisions and damage routing), camera, network sync, scoring | `World` mixes six concerns. Every co-op rule touched it, and every new mechanic will too | TEC-03; makes most WPN, ENM, LVL and COP rows cheaper | M |
| 2 | **Data-driven content:** weapon, projectile, enemy and pickup definitions as data (C# records or JSON/ScriptableObjects) instead of `switch` statements on enums | Adding one weapon touches `Projectile`, `Player`, `World` and `Art`. Network messages send enum bytes, which should become stable data IDs | TEC-02, WPN-02…12, ENM-02…10, CHR-02 | M |
| 3 | **Dynamic network IDs:** the authority allocates IDs; level spawns are just one source | Today an enemy's network identity is its index in `Level.Spawns`, so nothing spawned at runtime can be networked | TEC-05, ENM-05/06, WPN-13, POW-02/05, AIM-05 | M |
| 4 | **Scalable replication:** per-object `NetworkObject`s for big things (bosses, vehicles, lifts), and paged or generic tables for many small things | The world snapshot is a fixed 24-enemy array, and crate state is a 64-bit mask | TEC-06, BOS-03/04, LVL-07/09/10, VEH-* | M–L |
| 5 | **Assembly definitions:** `PooketSlug.Core` (no Fusion), `PooketSlug.Net` (Fusion; `defineConstraints: FUSION2`; listed in `AssembliesToWeave`), `PooketSlug.Editor` | The compiler would enforce the game/netcode boundary instead of `#if`. It also speeds up compiles and limits what the weaver touches | TEC-04 | S |
| 6 | **Typed network events:** one place that defines each event's payload, and smaller interfaces (`IAvatarChannel`, `IWorldChannel`) instead of one wide `INetLink` | Each new event is spread over `INetLink`, `NetSession`, the RPC and `World` | Every Medium-netcode row | S–M |
| 7 | **N players:** player count, colours, HUD and camera width from data; per-player state (score, weapon, character) in `PlayerNet` | Two players are assumed in `MaxPlayers`, the two skins, the partner HUD and `CoopHalfWidth` | COP-04, SCR-06, CHR-02/03, WPN-11 | M |
| 8 | **Per-player input sources:** keyboard sets, gamepads and touch behind one interface, with Input System actions built in code | `GameInput` is one global snapshot of hard-coded keys | COP-05, UIX-03, ACC-02/03 | M |
| 9 | **Split `Game` into flow and views,** and move to uGUI or UI Toolkit | `Game` holds the state machine, the HUD, the lobby, code entry and the session lifecycle. IMGUI also lacks glyphs and isn't meant for game UI | UIX-01…06, NET-03/04, CHR-03 | M |
| 10 | **Pooling:** `ObjectPool<T>` for projectiles, particles, effects and popups | Every bullet creates and destroys a GameObject, which churns the garbage collector on WebGL | TEC-01, PRE-04 | S |
| 11 | **Levels as data:** a level loader (LDtk or JSON), several levels, and the level ID in the shared world state | One hard-coded ASCII map | LVL-02/03/05/14, MOV-02/09 | M |
| 12 | **Tuning as data:** speeds, cooldowns, camera and netcode constants in one tunable place | Magic numbers are spread across classes | Balancing work, SCR-04, ENM-13 | S |
| 13 | **A save and profile interface,** local now and cloud later | High score is a lone `PlayerPrefs` key; the Google sign-in spike needs a seam | TEC-07, SOC-02, SCR-07…09 | S |
| 14 | **Build and CI:** split `WebGLBuild` into config, player settings and build steps, parameterised by target; add a test job and a fast pre-build check; per-branch concurrency and previews | One script and one job for one target | OPS-01…04, ACC-05/06, TEC-08 | M |
| 15 | **Testable core:** move tile physics, level parsing, pose and snapshot packing and room-code parsing into pure classes with EditMode tests | None of the logic is tested, and network packing is easy to break silently | TEC-08 | M |
| 16 | **Logging:** a small logger with categories and levels (`[PooketSlug/Net]` and so on) | Ad-hoc `Debug.Log`; network diagnostics can't be turned up or down | Support and debugging | S |
| 17 | **Seeded randomness:** a random-number source per system | `UnityEngine.Random` is used ad hoc. That's fine today, but replays, procedural levels and determinism need seeds | TEC-09, LVL-14, VRS-03/06 | S |

## Suggested order

This was the starting point, before the MoSCoW results. The [refactor plan](refactor-plan.md) replaces it.
1. **Changes that pay off for any roadmap:** 5 (assemblies), 10 (pooling), 15 (tests around what's about to move), 16 (logging). They're small, cheap and safe.
2. **The structural core:** 1 (split `World`), 2 (data-driven content), 6 (typed events).
3. **Then the scale limits the chosen features need:** 3 and 4 (identity and replication), 7 (N players), 11 (levels).
4. **Then the user-facing foundations:** 9 (UI), 8 (input), 13 (saves, alongside the Google sign-in spike), 14 (build and CI).

## Constraints to keep

- **Solo still builds and plays without the Photon SDK.**
- **The project stays code-only** unless a content pipeline is deliberately adopted (LVL-03, PRE-02).
- **The weaver config lists every assembly with networked types.** Re-run the [weaver check](../verification/fusion-weaver.md) after moving code.
- **A refactor must not change network behaviour without a two-player test** ([steps](../netcode/sessions.md#testing-by-hand)).
