# The GitHub Actions workflow

A walkthrough of [`.github/workflows/webgl-pages.yml`](../../../.github/workflows/webgl-pages.yml): what each step does, why it's there, and what went wrong while getting it right.

**Read this when** you're editing the workflow, adding a secret, or reading a CI failure.

Up: [Build pipeline](README.md)

## Triggers, permissions and concurrency

- **Triggers:**
  - Pushes to `main` and `claude/**`, and manual dispatch.
  - `paths-ignore` skips pushes that only touch `**.md` or `docs/**`. A doc change never redeploys the game, and a redeploy changes the build version, which [splits co-op players](../netcode/sessions.md#build-versions).
- **`permissions: contents: write`:** needed by `peaceiris/actions-gh-pages` to push the `gh-pages` branch.
- **Concurrency:** `group: webgl-pages` with `cancel-in-progress: false` (since 2026-10-16), so a build in flight always finishes and deploys; pushes that land meanwhile queue, and only the newest of them runs next.
  - The group is **global**: a build for any branch waits for any other. With `cancel-in-progress: true`, as before, several threads pushing to the same branch kept cancelling each other and nothing deployed for over an hour.

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

## Steps

### 1. Free disk space

- `jlumbroso/free-disk-space@v1.3.1` removes Android, .NET, Haskell and pre-pulled Docker images (`tool-cache: false`, `large-packages: false`, `swap-storage: false`).
- The `unityci/editor` WebGL image is about 7 GB compressed.
- It took 51 to 145 s across measured runs. We never tested whether today's runner would manage without it.

### 2. Checkout

`actions/checkout@v5` with default settings. No submodules and no LFS.

### 3. Fetch Photon Fusion SDK

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

### 5. EditMode tests

Four steps run the [EditMode tests](../verification/edit-mode-tests.md) before the build:
1. **Pick the editor image:** `unityci/editor:ubuntu-<version>-webgl-3`, with the version read from `ProjectVersion.txt`. The builder picks the same image, so it's pulled once.
2. **Run the tests** with `game-ci/unity-test-runner@v4.3.2` ([why that version](#action-versions)):

   | Input | Value | Why |
   |---|---|---|
   | `customImage` | The image from step 1 | One pull for both Unity runs |
   | `testMode` | `editmode` | The only tests so far |
   | `artifactsPath` | `test-results` | Where the NUnit XML and Unity's log go |

   - It activates Unity with the same three secrets as the build.
   - There's no `githubToken`, so it creates no check run, which would need `checks: write`. The summary covers it.
   - It always passes `-enableCodeCoverage`. Without the coverage package, Unity writes no coverage report.
   - A failing test fails the step, so the build and deploy don't run.
3. **Summarise the results:** the counts, and the first line of each failure.
4. **Upload the NUnit XML** as the `editmode-tests` artifact.

### 6. The build step

`game-ci/unity-builder@v4`, with the three Unity secrets passed as `env`:

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

After the build, the job serves it on `127.0.0.1:8000` and runs [`tools/demos.mjs`](../../../tools/demos.mjs): headless Chromium opens `?demo=all`, the game plays every move's scripted scene in turn ([`KitGuide.cs`](../../../Assets/Scripts/Core/KitGuide.cs)) on a fixed 30 fps step, and records its own canvas from each demo's first frame to its last ([`PooketDemo.jslib`](../../../Assets/Plugins/WebGL/PooketDemo.jslib), `MediaRecorder`; the first cut recorded the whole page and cut it at logged times, which drifted by seconds). The script collects each recording, speeds it back up to real time (SwiftShader draws slower than 30 fps) and encodes it with ffmpeg (installed by the step) as 640×360 H.264. The clips go into the build's `demos/` before it's uploaded and deployed, so they always match the build; they're also uploaded as the `kit-demos` artifact, with `demos.json` and the console log. The step has `continue-on-error`: without clips the build still deploys, and the kit screen says there's no demo yet. Added 2026-10-16.

### 7. Upload build artifact

`actions/upload-artifact@v6` uploads `build/WebGL/WebGL` as `PooketSlug-WebGL`. Set `if-no-files-found: error`.

### 8. Deploy to `gh-pages`

`peaceiris/actions-gh-pages@v4` with `force_orphan: true`: each deploy is a single fresh commit on `gh-pages`. See [GitHub Pages](github-pages.md#deploying).

### 9. Summary

Writes the commit, Unity version, build size and live URL to the job summary.

## The two-player co-op test job

A second job, `coop-test`, runs after the build on every push. It tests co-op on the exact build just made:
1. **It downloads the build artifact** and serves it on the runner with `python3 -m http.server`, so it doesn't wait for GitHub Pages to update.
2. **It installs Playwright 1.56.1 and Chromium.**
3. **It runs [`tools/coop.mjs`](../../../tools/coop.mjs)** in two separate headless Chromium instances. A hosts, B joins with the invite link, both play, then A leaves and B must take over the shared world ([details](../verification/browser-tests.md#two-browser-co-op-test)). GitHub's runners can open the WebSockets Photon needs; the cloud sandbox can't.
4. **It runs `coop.mjs` again in boss mode** (`?test=boss`): both start at Mission one's boss and fight it, and B must see the boss tank swing its turret and fire from A's world (its network object). Its results land in `coop-test/boss`.
5. **The job summary lists each check of both runs,** and how many pickups were given out. The `coop-test` artifact keeps both players' screenshots and console logs, even when the test fails.

- **It runs after the deploy, not before,** so a failure doesn't hold back the live build. It marks the push red instead.
- **Each run briefly uses two of the free Photon plan's 20 concurrent users.**

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
- **Third-party actions are pinned by tag, not SHA.** See [next steps](../next-steps.md).

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
