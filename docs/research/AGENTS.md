# Pooket Slug research: start here

Research notes and a reference guide for the pooket-slug spike. It covers a Unity 6.3 WebGL game built by GameCI on GitHub Actions and published to GitHub Pages, with online two-player co-op on Photon Fusion 2. This file is the table of contents: read it first, then open only what your task needs.

## How these docs are organised

They use **progressive disclosure**:
1. **This file:** what exists, the current status, and where to look.
2. **Topic overviews** (`<topic>/README.md`): one page per topic, enough to work at a high level.
3. **Leaf documents:** the detail, opened only when a task needs it.

Every document opens with a summary, a **Read this when** line and an **Up** link. It links to its neighbours and to the source files it describes. **The code is the source of truth;** these docs explain why it is the way it is, and what was tried and measured.

## Status (2026-10-14)

| Area | State | Where |
|---|---|---|
| GameCI → WebGL → GitHub Pages | ✅ Live; each push redeploys in 6–8 min | [build-pipeline](build-pipeline/README.md) |
| Online co-op (Fusion 2.1.3, Shared Mode) | ✅ Live; played by the owner on 2026-10-14 | [netcode](netcode/README.md), [test log](verification/test-log.md) |
| Verification without a Unity license | ✅ Working toolkit, re-tested 2026-10-14 | [verification](verification/README.md) |
| Feature roadmap | ✅ Sorted by the owner on 2026-10-14 (MoSCoW) | [roadmap](roadmap/README.md) |
| Refactor pass | ⏳ Plan proposed; waiting for the owner's go-ahead | [refactor plan](roadmap/refactor-plan.md) |
| "Sign in with Google" spike | ⏳ Reusing the Cardmaster Firebase project. Database rules drafted and tested, not yet committed; to be extended for the game design first. Waiting on the owner: permission to commit to Cardmaster, the console steps, the repository variables | [accounts](accounts/README.md), [todo](todo.md) |

**Live build:** https://jhiggins-tech.github.io/pooket-slug/ · **What's next:** [todo.md](todo.md) · **Game design:** [DESIGN.md](../../DESIGN.md), written with the owner

## Contents

- [lessons-learned.md](lessons-learned.md): the findings in one page, each linked to its evidence. It replaces the old root-level `LEARNED-LESSONS.md`.
- [recipe.md](recipe.md): the minimum steps to reproduce the pipeline, then add Fusion co-op.
- [todo.md](todo.md): **the working checklist**: the owner's and Claude's items, in order, with what each one waits on.
- [next-steps.md](next-steps.md): longer-term ideas for the pipeline, code and gameplay.
- [build-pipeline/](build-pipeline/README.md): how a push becomes a playable page.
  - [workflow.md](build-pipeline/workflow.md): every workflow step, secrets, caching, concurrency, reading CI logs.
  - [unity-project.md](build-pipeline/unity-project.md): the Unity version and image, the code-only project, packages and modules, input, `WebGLBuild.cs`.
  - [github-pages.md](build-pipeline/github-pages.md): deploying, compression, the WebGL template, caching.
  - [numbers.md](build-pipeline/numbers.md): build times and download sizes per run, before and after Fusion.
- [netcode/](netcode/README.md): online two-player co-op with Photon Fusion 2.
  - [choosing-a-stack.md](netcode/choosing-a-stack.md): Photon's SDKs compared, the 2D frameworks check, the free tier and licensing.
  - [sdk-in-ci.md](netcode/sdk-in-ci.md): the private SDK release, unpacking without Unity, Fusion's defines and weaver, upgrading.
  - [architecture.md](netcode/architecture.md): authority, replicated state, RPCs, prefab-less network objects, the sync loop, host migration.
  - [sessions.md](netcode/sessions.md): hosting, room codes, joining, quick match, failures, console logs, testing by hand.
- [verification/](verification/README.md): checking work without Unity or a license.
  - [type-check.md](verification/type-check.md): compiling against the GameCI image's own DLLs, and emulating disabled modules.
  - [fusion-weaver.md](verification/fusion-weaver.md): running Fusion's IL weaver outside Unity.
  - [browser-tests.md](verification/browser-tests.md): Playwright smoke and two-browser co-op tests, and the sandbox's network limits.
  - [test-log.md](verification/test-log.md): what was tested, when, how, and the result.
  - [tools/](verification/tools/README.md): the scripts (`extract-unity.sh`, `compile_unity.py`, `weave.sh`, `smoke.mjs`, `coop.mjs`, …).
- [accounts/](accounts/README.md): the "Sign in with Google" spike: Google's button plus Firebase over REST (in the shared Cardmaster project) to keep stats and progression per player, with no server.
  - [gcloud-setup.md](accounts/gcloud-setup.md): the console changes, the values to hand back, the trade-offs of sharing, and the separate-project alternative.
  - [database-rules.md](accounts/database-rules.md): this game's section of the shared database rules: the player record, how the game writes it, the tests, and running them in the sandbox.
- [roadmap/](roadmap/README.md): what to build next. The game design itself is [DESIGN.md](../../DESIGN.md) at the repository root.
  - [feature-candidates.csv](roadmap/feature-candidates.csv): 176 candidate mechanics and engineering items, with a MoSCoW column for the owner.
  - [refactor-candidates.md](roadmap/refactor-candidates.md): maintainability work, mapped to the features it unlocks.
  - [refactor-plan.md](roadmap/refactor-plan.md): the proposed refactor, ordered by the MoSCoW results and the design, with the decisions it needs.

## Quick facts

| | |
|---|---|
| Unity | 6000.3.26f1 (6.3 LTS), WebGL, C# 9 |
| CI image | `unityci/editor:ubuntu-6000.3.26f1-webgl-3` |
| Builder | `game-ci/unity-builder@v4`, `buildMethod: PooketSlug.EditorTools.WebGLBuild.Build` |
| Netcode | Photon Fusion 2.1.3 Stable (build 2390), Shared Mode, 2 players |
| Repository secrets | `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`, `VENDOR_PAT`, `PHOTON_APP_ID` |
| Private SDK storage | Release `fusion-2.1.3` in `jhiggins-tech/pooket-slug-vendor` |
| Working branch | `claude/gallant-keller-3qzp97`; every successful build deploys to `gh-pages` |
| Build triggers | Pushes to `main` / `claude/**`, except pushes that only touch `**.md` or `docs/**` |

## Keeping these docs useful

- **Put each fact in one leaf document,** and link to it from elsewhere instead of repeating it.
- **Date your measurements and cite CI run IDs,** so they can be checked.
- **When you add a document,** add it to its topic `README.md` and to the contents above.
- **Record test results** in [test-log.md](verification/test-log.md).
- **Never paste Photon SDK code, secret values, or private-repo code here.** This repository is public, and the SDK is licensed.
- **Never name the hidden ranks.** Of the rank ladder's 33 ranks, only Bronze, Silver, Gold, Platinum, Diamond, Master, Grandmaster and Champion may be named. Don't name or describe the other 25 in docs, tests or commit messages ([the owner's rule](../../DESIGN.md#ranks)).
