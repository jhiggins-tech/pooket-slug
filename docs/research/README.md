# Pooket Slug research

How the pipeline, the netcode, sign-in and the checks work, and what was tried and measured on the way. The code map, the working rules and the recipes are in the root [CLAUDE.md](../../CLAUDE.md); the game design is [docs/design](../design/README.md).

Every document opens with a summary, a **Read this when** line and an **Up** link. **The code is the source of truth;** these docs explain why it is the way it is.

**Live build:** https://jhiggins-tech.github.io/pooket-slug/ · **What's next:** [the Roadmap](https://claude.ai/artifact/1AWnQDGtUmiKMnjPgcbru9)

## Contents

- [build-pipeline/](build-pipeline/README.md): how a push becomes a playable page.
  - [workflow.md](build-pipeline/workflow.md): every workflow step, secrets, caching, concurrency, reading CI logs.
  - [unity-project.md](build-pipeline/unity-project.md): the Unity version and image, what's committed, packages and modules, input, `WebGLBuild.cs`.
  - [github-pages.md](build-pipeline/github-pages.md): deploying, compression, the WebGL template, caching.
- [netcode/](netcode/README.md): online two-player co-op with Photon Fusion 2.
  - [sdk-in-ci.md](netcode/sdk-in-ci.md): the private SDK release, unpacking without Unity, Fusion's defines and weaver, upgrading.
  - [architecture.md](netcode/architecture.md): authority, replicated state, RPCs, prefab-less network objects, the sync loop, host migration.
  - [sessions.md](netcode/sessions.md): the one co-op room (MATES), joining and watching it, build versions, hidden tabs, failures, console logs, testing by hand.
- [verification/](verification/README.md): checking work without Unity or a license.
  - [type-check.md](verification/type-check.md): compiling against the GameCI image's own DLLs, and emulating disabled modules.
  - [fusion-weaver.md](verification/fusion-weaver.md): running Fusion's IL weaver outside Unity.
  - [edit-mode-tests.md](verification/edit-mode-tests.md): the Core assembly's NUnit tests, how CI and you run them, and the bugs they caught on purpose.
  - [browser-tests.md](verification/browser-tests.md): Playwright smoke and two-browser co-op tests, and the sandbox's network limits.
  - [tools/](verification/tools/README.md): the scripts (`extract-unity.sh`, `compile_unity.py`, `weave.sh`, `run_tests.py`, `smoke.mjs`, …). The two-player test, `tools/coop.mjs`, runs in CI.
- [history/](history/): finished research, kept as a record and not as instructions.
  - [lessons-learned.md](history/lessons-learned.md): the spike's findings (14 to 16 Oct 2026).
  - [accounts-decisions.md](history/accounts-decisions.md): the sign-in options weighed, the effort estimate and the separate-project alternative.
- [accounts/](accounts/README.md): optional "Sign in with Google" (live): Google's button plus Firebase over REST (in the shared Cardmaster project) to keep a player's save, with no server.
  - [gcloud-setup.md](accounts/gcloud-setup.md): the console changes, the values to hand back, and the trade-offs of sharing.
  - [database-rules.md](accounts/database-rules.md): this game's section of the shared database rules: the player record, how the game writes it, the tests, and running them in the sandbox.

## Quick facts

Where a fact has an owner, it is linked rather than copied.

| | |
|---|---|
| Unity | [`ProjectSettings/ProjectVersion.txt`](../../ProjectSettings/ProjectVersion.txt) (WebGL, C# 9) |
| Builder | `game-ci/unity-builder` with `buildMethod: PooketSlug.EditorTools.WebGLBuild.Build` ([build script](build-pipeline/unity-project.md#the-build-script)) |
| Tests | EditMode and PlayMode (`game-ci/unity-test-runner`), the touch play test, the kit demo recording, and the two-player co-op test in 7 scenarios ([the jobs](build-pipeline/workflow.md#the-jobs)) |
| Netcode | Photon Fusion 2, Shared Mode, 2 players and up to 4 watching; the version and checksum are in [`action.yml`](../../.github/actions/unity-project/action.yml) |
| Repository secrets | `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`, `VENDOR_PAT`, `PHOTON_APP_ID`; variables `GOOGLE_CLIENT_ID`, `FIREBASE_API_KEY`, `FIREBASE_DB_URL` ([secrets](build-pipeline/workflow.md#secrets)) |
| Private SDK storage | The `jhiggins-tech/pooket-slug-vendor` repository ([SDK in CI](netcode/sdk-in-ci.md)) |
| Pipeline behaviour | The header of [`webgl-pages.yml`](../../.github/workflows/webgl-pages.yml): which branch deploys where, what triggers a build (the paths-ignore list is there too), what runs |

## Keeping these docs useful

- **Put each fact in one leaf document,** and link to it from elsewhere instead of repeating it.
- **Date your measurements and cite CI run IDs,** so they can be checked.
- **When you add a document,** add it to its topic `README.md` and to the contents above.
