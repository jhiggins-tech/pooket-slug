# Verification tools

The scripts behind the [verification](../README.md) docs. Agents run [`tools/check.sh`](../../../../tools/check.sh) at the repository root, not these. Each script's header comment says how to call it.

**Read this when** you want to know what a script here does, or you're changing one.

Up: [Verification](../README.md)

## Called by `tools/check.sh`

| File | What it does |
|---|---|
| [`compile_unity.py`](compile_unity.py) | Compiles the Photon SDK, the project's assembly definitions (Core and its tests) and the game's assemblies as Unity would for WebGL, against only the enabled engine modules. Writes the PDB, reference list and defines the weaver needs ([type-check.md](../type-check.md)) |
| [`weave.sh`](weave.sh) + [`WeaveHarness.cs`](WeaveHarness.cs) | Runs Fusion's `ILPostProcessor` over the netcode assembly outside Unity ([fusion-weaver.md](../fusion-weaver.md)) |
| [`WeaveDump.cs`](WeaveDump.cs) | Prints what the weaver generated for named types. Run via `DUMP="PlayerNet WorldNet" weave.sh ...` |
| [`run_tests.py`](run_tests.py) | Runs the [EditMode tests](../edit-mode-tests.md) outside Unity with NUnitLite on the kit's .NET runtime |

## Called by the vendor repository's Unity kit workflow

These make the kit that `check.sh` downloads. Agents never run them: the first one streams a 7 GB editor image.

| File | What it does |
|---|---|
| [`extract-unity.sh`](extract-unity.sh) | Streams the GameCI editor image's editor layer from Docker Hub and keeps Unity's compiler, reference DLLs and bundled packages |
| [`build-ugui.sh`](build-ugui.sh) | Compiles uGUI's runtime sources into `UnityEngine.UI.dll` |
| [`build-inputsystem.sh`](build-inputsystem.sh) | Compiles the Input System package's runtime sources into `Unity.InputSystem.dll` |

## The odd ones

| File | What it does |
|---|---|
| [`smoke.mjs`](smoke.mjs) | Playwright: loads a deployed build, plays a few seconds with scripted input, saves screenshots |
| [`auth-page.mjs`](auth-page.mjs) | Playwright: the page's sign-in and cloud saves (`pooket-auth.js`) against the database emulator, with fake Google and Auth servers |
| [`fetch-docs.mjs`](fetch-docs.mjs) | Playwright: renders documentation pages behind a JavaScript bot challenge (Photon's docs) and saves their text |
| [`rules-test.sh`](rules-test.sh) | Runs Cardmaster's database rules tests, which cover this game's `pooketSlug` section, against the emulator ([why](../../accounts/database-rules.md#running-the-tests-in-the-cloud-sandbox)) |

The browser tests that CI runs, [`touch.mjs`](../../../../tools/touch.mjs), [`coop.mjs`](../../../../tools/coop.mjs) and [`demos.mjs`](../../../../tools/demos.mjs), live in `tools/` ([browser-tests.md](../browser-tests.md)).

Playwright needs Chromium. In the cloud sandbox it's preinstalled: set `PW_PATH=$(npm root -g)/playwright` and don't run `playwright install`.
