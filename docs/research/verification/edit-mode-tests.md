# EditMode tests

The game's pure logic lives in its own assembly, `PooketSlug.Core`, and NUnit tests cover it. CI runs them in Unity's Test Framework before every build. Locally they run outside Unity, on the .NET runtime inside the extracted editor.

**Read this when** you're adding logic to Core, writing a test, or a test fails in CI.

Up: [Verification](README.md)

## What's in Core

[`Assets/Scripts/Core`](../../../Assets/Scripts/Core) holds the code that has no `MonoBehaviour`, no scene objects and no netcode, and calls nothing in the engine's native side except logging. It's an assembly definition with `autoReferenced`, so the rest of the game (`Assembly-CSharp`) sees it as before. Refactor plan step 2 moved into it the logic that later steps will move around.

| Class | What it does | What the tests pin down |
|---|---|---|
| `Level` | Parses a map into tiles, spawns and the player's start. (`LevelView`, outside Core, draws it) | Row order, markers, short rows, the walls and floor outside the map, solidity, top surfaces. Also that the mission starts on the ground, ends at the flag, and has at most 64 spawns, since the shared world's pickup mask has one bit per spawn |
| `TilePhysics` | Moves a box through the tile grid, with one-way bridges | Landing, falling, walls on both sides, the level's edges, ceilings, and bridges from above, below and inside. Also dropping through, and standing on a bridge |
| `AvatarPose` | A soldier's pose, packed into the int the network sends | Every pose survives packing, and the packed form fits in 10 bits |
| `InviteCode` | Room codes such as `ABCD-EU` | New names (no I or O), parsing, formatting, the round trip |
| `Rng` | Seeded xorshift32 | The exact sequence (replays will depend on it), ranges, a zero seed |
| `PageQuery` | Reads the page address | Parameters, case, escapes, `+`, fragments, missing names |
| `Log` | Log channels and the `?log=` spec | Parsing, configuring channels, levels |
| `Tuning` | The game's feel, as constants | Used by the physics tests, so they test the real soldier's size |

The tests are in [`Assets/Tests/EditMode`](../../../Assets/Tests/EditMode): 7 fixtures, 101 test cases, under a tenth of a second.

## In CI

The build job runs [`game-ci/unity-test-runner@v4`](../build-pipeline/workflow.md#5-editmode-tests) after restoring the Library cache and before the build:
- **It uses the build's editor image,** picked from `ProjectVersion.txt`, so the image is pulled once.
- **A failing test fails the job,** so nothing deploys.
- **The job summary lists the counts and any failures.** The NUnit XML uploads as the `editmode-tests` artifact.
- **No check run** (`githubToken: ''`), which would need the `checks: write` permission. Code coverage is off.

## Locally

Two steps, after the [type-check setup](tools/README.md):

```sh
# 1. Compile the tests against Unity's own NUnit, as the editor does
python3 -I $T/compile_unity.py --editor-data $D --project . --out $W/out \
        --inputsystem $W/is/Unity.InputSystem.dll --nunit $W/ext-nunit/package/net40/unity-custom/nunit.framework.dll game
# 2. Run them with NUnitLite on the editor's .NET runtime
python3 -I $T/run_tests.py --editor-data $D --project . --core $W/out/game/PooketSlug.Core.dll \
        --nunit $W/nunit/lib/netstandard2.0 --nunitlite $W/nunitlite/lib/netstandard2.0 --out $W/tests
```

- **Step 1's NUnit** is `com.unity.ext.nunit` 2.0.5, a custom build of NUnit 3.5. Get it from Unity's package registry: `https://download.packages.unity.com/com.unity.ext.nunit/-/com.unity.ext.nunit-2.0.5.tgz`, which redirects to a CDN (use `curl -L`). Check it against the registry's SHA-1, `60ef35ff…`.
  - It targets .NET Framework, so `compile_unity.py` also passes the editor's `NetStandard/compat/2.1.0/shims/netfx` facades, as the editor does.
- **Step 2's NUnit** is NuGet's `nunit` and `nunitlite` 3.14.0, from `https://api.nuget.org/v3-flatcontainer/<id>/3.14.0/<id>.3.14.0.nupkg`. Their SHA-512 matches NuGet's catalog.
  - `run_tests.py` compiles the same test sources against them with an NUnitLite `Main`, and runs that. It exits with the number of failed tests.
- **Only pure code runs outside Unity.** A test that reaches the engine's native side (`Debug.Log`, `GameObject`, `Time`) throws there. That's one more reason to keep such code out of Core.

## Do the tests catch bugs?

On 2026-10-14, Core was broken in eleven ways, one at a time. Each time it was recompiled and the tests rerun. All eleven were caught:

| Deliberate bug | Caught by |
|---|---|
| A bridge catches a soldier from inside it | `TilePhysicsTests.ABridgeDoesNotCatchYouFromInside` |
| A wall stops a soldier 0.1 tiles short | `TilePhysicsTests.RunningIntoAWallStops` |
| Sandbags next to a bridge count as bridge | `TilePhysicsTests.OnBridgeIsOnlyTrueOverABridgeAlone` |
| Map rows read bottom-up | 15 tests across `LevelTests` and `TilePhysicsTests` |
| Spawns left unsorted | `LevelTests.SpawnsAreSortedLeftToRight`, `TheMissionStartsOnTheGroundAndEndsAtTheFlag` |
| No walls at the level's edges | `LevelTests.OutsideTheMapIsWallsAndFloorBelowAndSkyAbove`, `TilePhysicsTests.TheLevelsEdgesAreWalls` |
| Two pose flags swapped when unpacking | `AvatarPoseTests.EveryPoseSurvivesPacking` |
| A code's region not lower-cased | `InviteCodeTests.TryParseSplitsTheNameAndRegion` |
| A different xorshift constant | `RngTests.ItIsXorshift32`, `ASeedOfZeroStillRuns`, and two `InviteCodeTests` |
| `+` in the address not read as a space | `PageQueryTests.GetReadsAParameter` |
| A log channel's level off by one | `LogTests.AChannelReportsItsLevelAndAbove` |

## Writing a test

- **Put it in `Assets/Tests/EditMode`,** in namespace `PooketSlug.Tests`, and give it a `.meta` file (`python3 -I tools/new_meta.py <file>`).
- **Use plain NUnit:** `[Test]`, `[TestCase]`, `Assert`, `StringAssert`, `CollectionAssert`.
  - Unity's NUnit is 3.5, so later additions such as `Assert.Multiple` don't exist. Step 1 above catches them.
  - `[UnityTest]` and `LogAssert` come from the Test Framework's own assemblies. They aren't available locally, so tests in this assembly don't use them.
- **Draw small maps in the test itself.** Row 0 is the top.
- **Pin what other code depends on:** the random sequence, which replays will use, and the pose bits, which the network sends.
- **Reset any static state you change,** in a `[TearDown]`, as `LogTests` does for the log levels.
