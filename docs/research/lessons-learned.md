# Lessons learned

The spike's findings in one place, each linked to the document with the detail and evidence. This replaces the old root-level `LEARNED-LESSONS.md`.

**Read this when** you want the takeaways without the detail, or you're deciding whether to reuse this approach.

Up: [Research index](AGENTS.md)

## The two questions

1. **Can GameCI on GitHub Actions build a Unity WebGL game and publish it to GitHub Pages with little effort?** **Yes.** An empty repository became a public, playable Unity 6.3 build in one session. Every workflow run that finished passed, the first one included, and each push redeploys in 6–8 minutes. → [Build pipeline](build-pipeline/README.md)
2. **Can the same pipeline ship real-time two-player co-op on Photon's free tier, without committing Photon's SDK?** **Yes.** CI fetches the private SDK, weaves and builds it with no manual steps, and the repo owner played a live session on 2026-10-14. → [Netcode](netcode/README.md)

## Pipeline

- **GameCI v4 with a Personal license worked first time.** It needed three secrets and no activation debugging. → [workflow](build-pipeline/workflow.md#secrets)
- **A custom `buildMethod` is the control point.** One editor script creates the scene, sets PlayerSettings, stamps the commit into the version and bakes config. GameCI's `customParameters` passes anything else. → [build script](build-pipeline/unity-project.md#the-build-script)
- **A code-only project means no hand-written Unity YAML.** Only `ProjectVersion.txt`, the manifest, scripts and one template are committed; Unity generates the rest in CI. → [unity project](build-pipeline/unity-project.md#a-code-only-project)
- **Deploying through a `gh-pages` branch decouples deploys from `main`.** A feature branch can deploy without environment rules. → [pages](build-pipeline/github-pages.md#deploying)
- **Turn WebGL compression off for Pages.** Pages gzips on the fly: 35.4 MB becomes 10.4 MB over the wire with Fusion, 7.2 MB without. → [pages](build-pipeline/github-pages.md#compression), [numbers](build-pipeline/numbers.md)
- **Check the GameCI image exists before pinning a Unity version,** and **pin packages to the versions bundled in the editor**, so package resolution needs no registry. → [unity project](build-pipeline/unity-project.md#version-and-editor-image)
- **Not knowing Active Input Handling up front is fine** if input code supports both backends. → [input](build-pipeline/unity-project.md#input)
- **IMGUI's default font lacks glyphs** such as `∞` and `→`. Only the real build showed it. → [gotchas](build-pipeline/unity-project.md#gotchas)
- **The concurrency group is global, and each push cancels the run in flight.** → [workflow](build-pipeline/workflow.md#triggers-permissions-and-concurrency)

## Netcode

- **Use Fusion 2 Shared Mode for WebGL.** Client/Server modes aren't available there. Realtime 5 doesn't list WebGL, PUN 2 is in maintenance, and Quantum would mean a rewrite. → [choosing a stack](netcode/choosing-a-stack.md)
- **Don't adopt a 2D platformer framework at this size.** The hand-written systems fit owner-simulated co-op better. Do adopt pooling and a real UI system. → [frameworks check](netcode/choosing-a-stack.md#are-we-reinventing-the-wheel-the-2d-frameworks-check)
- **A license-restricted SDK can live in a private release.** CI fetches it with a read-only token, checks a SHA-256 and unpacks the `.unitypackage` with 50 lines of Python, before Unity starts. → [SDK in CI](netcode/sdk-in-ci.md)
- **Fusion's weaver works in a single CI pass.** We expected to need a warm-up build and committed ProjectSettings. Instead, batch-mode Unity force-recompiles as soon as Fusion's installer adds its defines. → [defines and weaver](netcode/sdk-in-ci.md#fusions-defines-and-the-il-weaver)
- **Network objects don't need prefabs.** Bake a code-built template and register it with `NetworkPrefabSourceStatic` under a fixed GUID. → [architecture](netcode/architecture.md#network-objects-without-prefabs)
- **Split authority by what must feel instant.**
  - Each client simulates its own soldier, so there's no input lag.
  - The master client simulates the world, so there's one truth, and it survives the host leaving.
  - A soldier's damage is judged on its own client, so dodging what you see works.

  → [authority model](netcode/architecture.md#authority-model)
- **Keep gameplay SDK-agnostic.** Interfaces, plus the Fusion code in its own assembly that only compiles with the SDK, mean the game builds without the private SDK. → [boundary](netcode/architecture.md#the-boundary-between-game-and-netcode)
- **Room codes must carry the region,** because sessions are per region. **A mistyped code silently creates an empty room** in Shared Mode, so check for it. → [sessions](netcode/sessions.md#room-codes)
- **Set `AppVersion` to the build version.** Mismatched builds never meet, which is also why doc-only pushes shouldn't redeploy. → [build versions](netcode/sessions.md#build-versions)
- **Browsers pause hidden tabs,** which freezes a co-op player. Test in two visible windows. → [hidden tabs](netcode/sessions.md#hidden-tabs)
- **Fusion costs about 3.2 MB of download and about a minute of build time.** → [numbers](build-pipeline/numbers.md)
- **The free 20 CCU plan is for development only.** Fusion apps qualify for the free 100 CCU commercial plan. → [free tier](netcode/choosing-a-stack.md#free-tier-and-licensing)

## Verification

- **Type-check against the GameCI image's own DLLs.** Stream the editor layer, keep about 300 MB, and compile with Unity's Roslyn and C# 9. → [type-check](verification/type-check.md)
- **Emulate disabled engine modules** when compiling. That's how the SDK's six extra module needs were found before CI. → [modules](verification/type-check.md#emulating-disabled-modules)
- **Run Fusion's IL weaver outside Unity.** A 70-line harness on the editor's .NET runtime turns weaver errors into a seconds-long local check. → [weaver](verification/fusion-weaver.md)
- **Headless Chromium with SwiftShader runs the real build** well enough for smoke tests and found the glyph bug. → [browser tests](verification/browser-tests.md)
- **The cloud sandbox can't open WebSockets,** so live co-op needs a normal network. → [limits](verification/browser-tests.md#the-sandboxs-limits)
- **Docker Hub limits anonymous manifest requests** to 100 per hour per IP, and shared IPs run out. → [type-check](verification/type-check.md#1-get-unitys-compiler-and-dlls-from-the-editor-image)
- **The Firebase CLI ignores `NO_PROXY`.** Behind the sandbox's proxy, even its calls to local emulators go through `HTTPS_PROXY` and fail. Start the database emulator's JAR directly instead. → [database rules](accounts/database-rules.md#running-the-tests-in-the-cloud-sandbox)
- **Mutation-test security rules.** Weaken each rule and check that a named test fails. Also check the test script's exit code: a cleanup trap under `set -e` had masked it. → [database rules](accounts/database-rules.md#tests)

## Working practices

- **Fail early and by name when a secret is missing.** A missing secret is an empty string, not an error. The `VENDOR_PAT` guard made a name mismatch a one-minute fix. → [secrets](build-pipeline/workflow.md#secrets)
- **Print one diagnostics line from the build script.** `[PooketSlug] Fusion SDK present / IL weaver compiled / defines / AppId` answers most CI questions without reading Unity's log. → [reading logs](build-pipeline/workflow.md#reading-ci-logs)
- **Re-run a workflow run after adding a secret.** No commit is needed.
- **Spot-check delegated research.** The frameworks check came from a background agent; the two claims its verdicts leaned on most were checked against their sources before being relied on. → [frameworks check](netcode/choosing-a-stack.md#are-we-reinventing-the-wheel-the-2d-frameworks-check)
- **Keep docs out of the build trigger.** `**.md` and `docs/**` are path-ignored, so research never redeploys the game. → [workflow](build-pipeline/workflow.md#triggers-permissions-and-concurrency)
