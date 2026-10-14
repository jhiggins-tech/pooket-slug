# Verification tools

The scripts behind the [verification](../README.md) docs. They're research tools, not part of the build; CI doesn't run them, and pushes to `docs/**` don't trigger a build. Each script's header comment says how to call it.

**Read this when** you want to run a check yourself.

Up: [Verification](../README.md)

| File | What it does | Tested |
|---|---|---|
| [`extract-unity.sh`](extract-unity.sh) | Streams the GameCI editor image's editor layer from Docker Hub and extracts Unity's Roslyn compiler, the .NET runtime, the reference DLLs, module metadata, and the bundled Input System, Cecil and uGUI packages (~300 MB written; ~7 GB streamed) | 2026-10-14: re-run for uGUI, about 20 minutes through the sandbox's proxy |
| [`build-ugui.sh`](build-ugui.sh) | Compiles uGUI's runtime sources (not its TextMeshPro half) into `UnityEngine.UI.dll` | 2026-10-14: 88 files, no errors |
| [`build-inputsystem.sh`](build-inputsystem.sh) | Compiles the Input System package's runtime sources into `Unity.InputSystem.dll`. With `UGUI=<UnityEngine.UI.dll>`, it also builds the UI support, as Unity does once uGUI is in the project | 2026-10-14: 281 files, warnings only, with and without the UI support |
| [`compile_unity.py`](compile_unity.py) | Compiles the Photon SDK (player and editor), the project's assembly definitions (Core and its tests) and the game's assemblies, as Unity would for WebGL, against only the enabled engine modules. Also writes the PDB, reference list and defines for the weaver. `--input legacy\|new\|both` picks the Active Input Handling to compile for, and `--ugui` adds uGUI | 2026-10-14: SDK OK; Core, tests and game OK, with and without the SDK, for each input backend |
| [`run_tests.py`](run_tests.py) | Runs the [EditMode tests](../edit-mode-tests.md) outside Unity: builds them against NuGet's NUnit 3.14 with an NUnitLite entry point, and runs them on the editor's .NET runtime | 2026-10-14: 175 of 175; 27 of 27 deliberate bugs caught (11 in the first tests, 10 in the touch pad, 6 in the profile) |
| [`weave.sh`](weave.sh) + [`WeaveHarness.cs`](WeaveHarness.cs) | Runs Fusion's `ILPostProcessor` (the IL weaver) over the game assembly outside Unity, on the editor's own .NET runtime | 2026-10-14: woven, no diagnostics |
| [`WeaveDump.cs`](WeaveDump.cs) | Prints what the weaver generated for named types: word counts, generated RPC invokers, getter IL. Run via `DUMP="PlayerNet WorldNet" weave.sh …` | 2026-10-14 |
| [`smoke.mjs`](smoke.mjs) | Playwright: loads a deployed build, plays a few seconds with scripted input, saves screenshots and console logs | Used against every deploy |
| [`coop.mjs`](../../../../tools/coop.mjs) (in `tools/`) | Playwright: two separate Chromium instances, where A hosts, B joins by invite link, both play, then A leaves and B should take over. Exits non-zero if a check fails | Runs in CI on every build ([the job](../../build-pipeline/workflow.md#the-two-player-co-op-test-job)) |
| [`fetch-docs.mjs`](fetch-docs.mjs) | Playwright: renders documentation pages behind a JavaScript bot challenge (Photon's docs site) and saves their text and links | Used for the Photon research |
| [`rules-test.sh`](rules-test.sh) | Runs Cardmaster's database rules tests, including this game's `pooketSlug` section, against the database emulator's JAR started directly. That works around the Firebase CLI ignoring `NO_PROXY` ([why](../../accounts/database-rules.md#running-the-tests-in-the-cloud-sandbox)) | 2026-10-14: 24 of 24, and 12 of 12 rule weakenings caught |

A full local check, assuming a scratch directory `W`:

```sh
T=docs/research/verification/tools
$T/extract-unity.sh ubuntu-6000.3.26f1-webgl-3 $W/unity            # once; ~7 GB streamed
D=$W/unity/opt/unity/Editor/Data
tar -xzf $D/Resources/PackageManager/Editor/com.unity.nuget.mono-cecil-*.tgz -C $W   # → $W/package/Mono.Cecil*.dll
$T/build-ugui.sh $D $W/ugui
UGUI=$W/ugui/UnityEngine.UI.dll $T/build-inputsystem.sh $D $W/is
python3 -I tools/unpack_unitypackage.py <Photon-Fusion-….unitypackage> .   # the SDK, if not already unpacked
python3 -I $T/compile_unity.py --editor-data $D --project . --out $W/out \
        --cecil-dir $W/package --inputsystem $W/is/Unity.InputSystem.dll --ugui $W/ugui/UnityEngine.UI.dll \
        --nunit $W/ext-nunit/package/net40/unity-custom/nunit.framework.dll all
DUMP="PlayerNet WorldNet" $T/weave.sh $D "$PWD" $W/out $W/package $W/weave
python3 -I $T/run_tests.py --editor-data $D --project . --core $W/out/game/PooketSlug.Core.dll \
        --nunit $W/nunit/lib/netstandard2.0 --nunitlite $W/nunitlite/lib/netstandard2.0 --out $W/tests
```

The two NUnit builds come from Unity's package registry and NuGet ([where](../edit-mode-tests.md#locally)).

Playwright needs Chromium. In the cloud sandbox it's preinstalled: set `PW_PATH=$(npm root -g)/playwright` and don't run `playwright install`. Elsewhere, run `npm i playwright && npx playwright install chromium`.
