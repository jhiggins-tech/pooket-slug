# EditMode tests

The game's pure logic lives in its own assembly, `PooketSlug.Core`, and NUnit tests cover it: 73 test files in `Assets/Tests/EditMode`, 795 tests, about a second to run. CI runs them in Unity's Test Framework before every build. Locally `tools/check.sh` runs them outside Unity, on the .NET runtime in the Unity kit.

**Read this when** you're adding logic to Core, writing a test, or a test fails in CI.

Up: [Verification](README.md)

## What's in Core

[`Assets/Scripts/Core`](../../../Assets/Scripts/Core) holds the code that has no `MonoBehaviour`, no scene objects and no Fusion types, and calls nothing in the engine's native side except logging. Its folders mirror the game's, and [CLAUDE.md's map](../../../CLAUDE.md#map) says what each holds. Core is also what the game and the netcode assembly (`PooketSlug.Net`) share: the contract between them (`NetContracts`, `INetEvents`, the event payloads) and `NetConfig`. The contract isn't tested here; `NetConfig`'s region rule is (`NetConfigTests`).

## In CI

The build job runs [`game-ci/unity-test-runner`](../build-pipeline/workflow.md#5-editmode-and-playmode-tests) after restoring the Library cache and before the build:
- **It uses the build's editor image,** picked from `ProjectVersion.txt`, so the image is pulled once.
- **A failing test fails the job,** so nothing deploys.
- **The job summary lists each mode's counts and any failures,** and the counts are a notice on the run too. The NUnit XML and Unity's log upload as the `tests` artifact.
- **No check run,** which would need the `checks: write` permission.

## Locally

```sh
tools/check.sh
```

That type-checks the tests against Unity's own NUnit (a custom NUnit 3.5, as the editor does), then runs them with NUnitLite 3.14 on the kit's .NET runtime. It exits with the number of failed tests. See [the tools](tools/README.md) for the scripts underneath.

- **Only pure code runs outside Unity.** A test that reaches the engine's native side (`Debug.Log`, `GameObject`, `Time`) throws there. That's one more reason to keep such code out of Core.

## Do the tests catch bugs?

Core was broken on purpose 32 ways, one at a time, and every bug was caught. The first round found gaps in the touch pad, input, profile and missions tests, which were then filled.
The pins that matter most are content ids, mission ids and net ids, and the pickup table's size.

## Writing a test

- **Put it in `Assets/Tests/EditMode`,** in namespace `PooketSlug.Tests`, and give it a `.meta` file (`python3 -I tools/new_meta.py <file>`).
- **Use plain NUnit:** `[Test]`, `[TestCase]`, `Assert`, `StringAssert`, `CollectionAssert`.
  - Unity's NUnit is 3.5, so later additions such as `Assert.Multiple` don't exist. `tools/check.sh` catches them.
  - `[UnityTest]` and `LogAssert` come from the Test Framework's own assemblies. They aren't available locally, so tests in this assembly don't use them.
- **Draw small maps in the test itself.** Row 0 is the top.
- **Pin what other code depends on:** the random sequence, which replays will use, and the pose bits, which the network sends.
- **Reset any static state you change,** in a `[TearDown]`, as `LogTests` does for the log levels.
