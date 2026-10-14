# The GitHub Actions workflow

A walkthrough of [`.github/workflows/webgl-pages.yml`](../../../.github/workflows/webgl-pages.yml): what each step does, why it's there, and what went wrong while getting it right.

**Read this when** you're editing the workflow, adding a secret, or reading a CI failure.

Up: [Build pipeline](README.md)

## Triggers, permissions and concurrency

- **Triggers:**
  - Pushes to `main` and `claude/**`, and manual dispatch.
  - `paths-ignore` skips pushes that only touch `**.md` or `docs/**`. A doc change never redeploys the game, and a redeploy changes the build version, which [splits co-op players](../netcode/sessions.md#build-versions).
- **`permissions: contents: write`:** needed by `peaceiris/actions-gh-pages` to push the `gh-pages` branch.
- **Concurrency:** `group: webgl-pages` with `cancel-in-progress: true`, so a new push cancels the build in flight.
  - The group is **global**, so a push to *any* branch cancels *any* in-flight build. That's fine for one branch; use `group: webgl-pages-${{ github.ref }}` before several people push.
  - Pushing a follow-up commit cancels the previous run. Push small changes early, or wait.

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

### 5. The build step

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

### 6. Upload build artifact

`actions/upload-artifact@v6` uploads `build/WebGL/WebGL` as `PooketSlug-WebGL`. Set `if-no-files-found: error`.

### 7. Deploy to `gh-pages`

`peaceiris/actions-gh-pages@v4` with `force_orphan: true`: each deploy is a single fresh commit on `gh-pages`. See [GitHub Pages](github-pages.md#deploying).

### 8. Summary

Writes the commit, Unity version, build size and live URL to the job summary.

## The two-player co-op test job

A second job, `coop-test`, runs after the build on every push. It tests co-op on the exact build just made:
1. **It downloads the build artifact** and serves it on the runner with `python3 -m http.server`, so it doesn't wait for GitHub Pages to update.
2. **It installs Playwright 1.56.1 and Chromium.**
3. **It runs [`tools/coop.mjs`](../../../tools/coop.mjs)** in two separate headless Chromium instances. A hosts, B joins with the invite link, both play, then A leaves and B must take over the shared world ([details](../verification/browser-tests.md#two-browser-co-op-test)). GitHub's runners can open the WebSockets Photon needs; the cloud sandbox can't.
4. **The job summary lists each check.** The `coop-test` artifact keeps both players' screenshots and console logs, even when the test fails.

- **It runs after the deploy, not before,** so a failure doesn't hold back the live build. It marks the push red instead.
- **Each run briefly uses two of the free Photon plan's 20 concurrent users.**

## Action versions

- **`unity-builder` v4 or v6:**
  - v6 (August 2026) is a thin wrapper around `game-ci/cli` on Node 24.
  - v4.8.x is the long-standing implementation on Node 20, and still gets backports.
  - We use v4. It logs a harmless "Node.js 20 is deprecated … forced to run on Node.js 24" warning.
  - Revisit v6 after a few point releases.
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
