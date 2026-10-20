# The GitHub Actions workflow

A walkthrough of [`.github/workflows/webgl-pages.yml`](../../../.github/workflows/webgl-pages.yml): what each step does, why it's there, and what went wrong while getting it right.

**Read this when** you're editing the workflow, adding a secret, or reading a CI failure.

Up: [Build pipeline](README.md)

## Triggers, permissions and concurrency

- **Triggers:**
  - Pushes to `main` and `claude/**`, branch deletions, and manual dispatch. A plan job decides what a push builds: the main line deploys to the root, other branches to a preview folder, and a commit the main line already has builds nothing. A preview runs the co-op tests only when the branch changes the netcode (`Net/`, `World/NetSync.cs`, `Core/Platform/NetContracts.cs`, `tools/coop.mjs`) and the kit demos only when it changes the crew (`Crew/`, `Core/Crew/KitGuide.cs`, `PooketDemo.jslib`, `tools/demos.mjs`), or this workflow; a commit message line `CI: co-op` or `CI: demos` asks for either. Spikes (`claude/spike-*`) skip both. The main line always runs everything.
  - `paths-ignore` skips pushes that only touch `**.md` or `docs/**`. A doc change never redeploys the game, and a redeploy changes the build version, which [splits co-op players](../netcode/sessions.md#build-versions).
- **Permissions:** the workflow's token is read-only (`contents: read`); the three jobs that push the `gh-pages` branch (`build-and-deploy`, `deploy-live`, `prune-previews`) ask for `contents: write` themselves.
- **Runners:** `ubuntu-24.04`, pinned, so an image change under `ubuntu-latest` can't change the build unannounced.
- **Concurrency:** a group per branch (`webgl-pages-<branch>`) with `cancel-in-progress: false`, so branches build side by side, and within a branch a build in flight always finishes and deploys; pushes that land meanwhile queue, and only the newest of them runs next.
  - Until 16 Oct the group was global, and with `cancel-in-progress: true` several threads pushing to the same branch kept cancelling each other and nothing deployed for over an hour.

## Secrets

All are repository secrets on `jhiggins-tech/pooket-slug` (Settings → Secrets and variables → Actions):

| Secret | Contents | Used by |
|---|---|---|
| `UNITY_LICENSE` | The contents of the Personal license `.ulf` file | GameCI activation |
| `UNITY_EMAIL`, `UNITY_PASSWORD` | The Unity account the license belongs to | GameCI activation |
| `VENDOR_PAT` | A fine-grained token, repository access **only** `pooket-slug-vendor`, permission **Contents: Read-only** | SDK fetch step ([detail](../netcode/sdk-in-ci.md)) |
| `PHOTON_APP_ID` | The **Fusion 2** AppId from the Photon dashboard | Baked into the build. Optional: without it the build passes but co-op is off |

How secrets behave:
- **A missing secret expands to an empty string, not an error.** The build step logged `customParameters: -buildSha … -photonAppId ` with nothing after the flag, and the build script reported `No -photonAppId: online co-op is off in this build`.
  - For secrets the build can't work without, fail early with a named error. The SDK step does: `::error::Repository secret VENDOR_PAT is not set`.
  - That guard made a secret-name mismatch obvious. The workflow read `VENDOR_TOKEN` while the secret had been created as `VENDOR_PAT`.
- **Re-running a workflow run uses the current secret values,** so a secret added after a run can be picked up with "Re-run all jobs". No new commit is needed.
- **Secret values are masked as `***` in logs.**

## The jobs

Since ci-faster (20 Oct) the Unity work is split so the slow parts run at once: `plan` → `scenes`, `tests` and `build` side by side → `touch` and `demos` side by side on the build → `build-and-deploy` (once scenes, tests, build and touch have passed; the demos are optional) → `coop-test` → `deploy-live` (main line). Before, one job ran tests, build, touch play and demos one after another.

- **`tests` and `build` get the project ready the same way**, with the composite action [`.github/actions/unity-project`](../../../.github/actions/unity-project/action.yml): steps 1 to 4 below, then the editor image's name. Only `build` saves the Library cache; `tests` restores it.
- **`build` uploads the raw build** as `unity-build` (kept a day) for `touch` and `demos`. `build-and-deploy` adds the demos, the sandbox's setups and the checked scenes, uploads it as `PooketSlug-WebGL` (what the co-op tests play and `deploy-live` deploys) and deploys a preview.
- **Playwright** (Node 22, Playwright 1.56.1, Chromium cached by version) is the composite action [`.github/actions/playwright`](../../../.github/actions/playwright/action.yml), used by `touch`, `demos` and `coop-test`.

## Steps

### 1. Room for the editor image

- The `unityci/editor` WebGL image is about 7 GB compressed, more than the runner's main disk has free.
- Docker's storage moves to the runner's second disk, `/mnt`, which is mostly empty: about 8 s.
- Until 17 Oct, `jlumbroso/free-disk-space@v1.3.1` deleted Android, .NET, Haskell and pre-pulled Docker images instead, which took 51 to 145 s. The `spike-ci-disk` run showed the second disk is enough.

### 2. Checkout

`actions/checkout@v5` with default settings. No submodules and no LFS.

### 3. Fetch Photon Fusion SDK

First, a check: if anything of Photon's is committed (anything under `Assets/Photon/` but our `NetworkProjectConfig.fusion`, a `.unitypackage`, or Fusion's DLLs), the job fails, as the SDK's licence forbids it in this public repository.

1. Downloads `Photon-Fusion-2.1.3-Stable-2390.unitypackage` from release `fusion-2.1.3` of the private repo `jhiggins-tech/pooket-slug-vendor`, using `gh release download` with `GH_TOKEN: secrets.VENDOR_PAT`.
2. Checks the file against a pinned SHA-256.
3. Unpacks it with `tools/unpack_unitypackage.py`.
4. Restores our committed `NetworkProjectConfig.fusion` over the package's copy.

It takes 1 to 2 s. Why it works this way, and how to upgrade the SDK: [SDK in CI](../netcode/sdk-in-ci.md).

### 4. Cache Unity Library

- `actions/cache@v5`, path `Library`.
- The key is `Library-WebGL-<hash of Assets/**, Packages/**, ProjectSettings/**>`, with the restore key `Library-WebGL-`.
- Any asset change misses the exact key, then restores the most recent Library and imports incrementally.
- Before Fusion, the cache was 83 MB compressed and saved about 70 s per build.

### 5. EditMode and PlayMode tests

The `tests` job runs the [EditMode tests](../verification/edit-mode-tests.md) and the PlayMode ones, beside the build:
1. **Pick the editor image:** `unityci/editor:ubuntu-<version>-webgl-3`, with the version read from `ProjectVersion.txt`; the builder is given the same one.
2. **Run the EditMode tests**, then 3. **the PlayMode tests**, each with `game-ci/unity-test-runner@v4.3.2` ([why that version](#action-versions)) in its own Unity and container. Run together (`testMode: all`), PlayMode could leave Unity's IL post-processing runner alive, and EditMode then died with "Cannot access a closed pipe", reported as "Scripts have compiler errors". The PlayMode step runs after a red EditMode one too, so a run shows both modes.

   | Input | Value | Why |
   |---|---|---|
   | `customImage` | The image from step 1 | One pull for both test modes |
   | `testMode` | `editmode`, then `playmode` | Core's rules, then gameplay (`Assets/Tests/Gameplay`) |
   | `artifactsPath` | `test-results/editmode`, `test-results/playmode` | Where each mode's NUnit XML and Unity's log go |

   - Each activates Unity with the same three secrets as the build.
   - There's no `githubToken`, so it creates no check run, which would need `checks: write`. The summary covers it.
   - It always passes `-enableCodeCoverage`. Without the coverage package, Unity writes no coverage report.
   - A failing test fails the job, so `build-and-deploy` doesn't run: nothing deploys.
4. **Summarise the results:** each mode's counts (or that a mode wrote none), and the first line of each failure. The counts are also a notice on the run, which the checks API reads (`check-runs/<id>/annotations`) when the log and artifact are out of reach.
5. **Upload the NUnit XML** as the `tests` artifact (both modes, in a folder each).

### 6. The build step

`game-ci/unity-builder@v4.8.2` (pinned: the floating `v4` tag could change under us), with the three Unity secrets passed as `env`:

| Input | Value | Why |
|---|---|---|
| `targetPlatform` | `WebGL` | Selects the `-webgl-3` editor image and `-buildTarget WebGL` |
| `buildMethod` | `PooketSlug.EditorTools.WebGLBuild.Build` | Our own entry point instead of GameCI's default builder ([build script](unity-project.md#the-build-script)) |
| `customParameters` | `-buildSha ${{ github.sha }} -photonAppId ${{ secrets.PHOTON_APP_ID }}` | Arbitrary flags passed to Unity; the build script reads them with `Environment.GetCommandLineArgs()` |
| `versioning` | `None` | The default, `Semantic`, needs full git history (`fetch-depth: 0`). We stamp the SHA ourselves |

- **Unity version:** read from `ProjectSettings/ProjectVersion.txt`.
- **Build path:** GameCI adds `-customBuildPath` (`build/WebGL/WebGL`) and `-buildTarget`.
- **Exit code:** the build method exits `0` on success and `101` on failure, which fails the step.

### 6b. The kit screen's demos

The `demos` job downloads the build, serves it on `127.0.0.1:8000` and runs [`tools/demos.mjs`](../../../tools/demos.mjs): headless Chromium opens `?demo=all`, the game plays every move's scripted scene in turn ([`KitGuide.cs`](../../../Assets/Scripts/Core/Crew/KitGuide.cs)) on a fixed 30 fps step, and records its own canvas from each demo's first frame to its last ([`PooketDemo.jslib`](../../../Assets/Plugins/WebGL/PooketDemo.jslib), `MediaRecorder`; the first cut recorded the whole page and cut it at logged times, which drifted by seconds). The script collects each recording, speeds it back up to real time (SwiftShader draws slower than 30 fps) and encodes it with ffmpeg (installed by the step) as 640×360 H.264. The clips are uploaded as the `kit-demos` artifact, with `demos.json` and the console log, and `build-and-deploy` puts them in the build's `demos/`, so they always match the build. The recording has `continue-on-error`: without clips the build still deploys, and the kit screen says there's no demo yet. Added 2026-10-16.

### 7. Upload build artifact

`build-and-deploy` uploads the build, with the demos, the sandbox's setups and the scenes, as `PooketSlug-WebGL`. Set `if-no-files-found: error`.

### 8. Deploy to `gh-pages`

`tools/pages_deploy.sh` swaps the build into its own place (the root, or `previews/<slug>/`), drops previews whose branch is gone or merged into the main line, and pushes `gh-pages` as a single fresh commit, leased against the one it started from. See [GitHub Pages](github-pages.md#deploying).

### 9. Summary

Writes the commit, Unity version, build size and live URL to the job summary.

## The two-player co-op test job

`coop-test` runs after the build, as one job per scenario, three at a time (a matrix). It tests co-op on the exact build just made:
1. **It downloads the build artifact** and serves it on the runner with `python3 -m http.server`, so it doesn't wait for GitHub Pages to update.
2. **It sets up Playwright** (the `playwright` action: 1.56.1, Chromium from the cache).
3. **It runs [`tools/coop.mjs`](../../../tools/coop.mjs)** in its scenario's mode, in separate headless Chromium instances: `code` (A hosts, B joins with the invite link, both play, then A leaves and B must take over the shared world: [details](../verification/browser-tests.md#two-browser-co-op-test)), `boss`, `scene`, `dropin` (a spike: its failure doesn't fail the job), `handover` and `watch` (three phones). GitHub's runners can open the WebSockets Photon needs; the cloud sandbox can't.
4. **Each job's summary lists its checks.** Its `coop-test-<mode>` artifact keeps the players' screenshots and console logs, even when the test fails.

- **On the main line, the live deploy waits for it.** The `deploy-live` job puts the build live only once every scenario has passed, so the live game never gets a build co-op failed. Previews deploy straight after the build.
- **Three at a time keeps Photon's free plan in reach:** at most 7 of its 20 concurrent users per branch (`watch` has three phones), so two branches testing at the same moment fit; a third could go over, and a round that can't get into its room fails.

## Action versions

- **`unity-builder` v4 or v6:**
  - v6 (August 2026) is a thin wrapper around `game-ci/cli` on Node 24.
  - v4.8.x is the long-standing implementation on Node 20, and still gets backports.
  - We use v4. It logs a harmless "Node.js 20 is deprecated … forced to run on Node.js 24" warning.
  - Revisit v6 after a few point releases.
- **`unity-test-runner` is pinned to v4.3.2.**
  - That's the last release that runs Unity itself.
  - v4.4.0, which the `v4` tag now points to, is a thin wrapper around the `game-ci/cli` binary, fetched at `latest`.
  - Our first test run used `v4` with `coverageEnabled: false`. The action passed `--no-coverageEnabled`, CLI v0.1.72 rejected it as an unknown argument, and the step failed before Unity started (run 38578138923).
- **Node 24 majors:** `actions/cache@v5`, `actions/upload-artifact@v6`, `actions/download-artifact@v8` and `actions/setup-node@v7` are already on Node 24.
- **Third-party actions are pinned by tag, not SHA.**

## Reading CI logs

- The build script prints `[PooketSlug]` lines that answer the usual questions without scrolling through Unity's log:

  ```text
  [PooketSlug] Photon AppId baked in; quick match region: best by ping
  [PooketSlug] Fusion SDK present: True; IL weaver compiled: True; co-op code compiled: True; WebGL defines: '…FUSION_WEAVER;FUSION2;…'
  ```

- `IL weaver compiled: False`, or no `FUSION2` in the defines, means the networking code wasn't woven or compiled. See [SDK in CI](../netcode/sdk-in-ci.md#fusions-defines-and-the-il-weaver).
- Unity's own summary is printed by the build method as `Result:`, `Duration:`, `Errors:`, `Warnings:`, `Size:`.
- To grep a whole run locally:
  - **With the `gh` CLI:** run `gh run view <run-id> --log > run.log`.
  - **With the API:** `GET /repos/{owner}/{repo}/actions/runs/{run_id}/logs` returns a signed zip URL.
- `ExitCode: 4` lines in Unity's log are Bee's "requires additional run" passes, not failures.
