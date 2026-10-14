# Next steps

What's queued, what's open, and what we'd do if this went further. Feature choices live in the [roadmap](roadmap/README.md); this page covers process, infrastructure and loose ends.

**Read this when** you're picking up work after a pause.

Up: [Research index](AGENTS.md)

## Queued (as of 2026-10-14)

1. **The MoSCoW feature roadmap.** The owner is sorting the [candidate list](roadmap/feature-candidates.csv) into Must, Should, Could and Won't.
2. **A refactor pass** that prepares the build and netcode for the chosen features. It's guided by the [refactor candidates](roadmap/refactor-candidates.md) and waits on step 1.
3. **A "Sign in with Google" spike:** identify players and keep their stats and progression across devices. The plan is Google's button plus Firebase over REST, with no server ([accounts](accounts/README.md)). It's waiting for the owner's [Firebase setup](accounts/gcloud-setup.md), and implementation comes after the roadmap.

## Loose ends

- **Clearer connection failures.** A failure while connecting reads `DISCONNECTED (ERROR)`; *Couldn't reach Photon* would say what happened. → [sessions](netcode/sessions.md#when-things-fail)
- **Confirm the untested co-op paths:** host migration, quick match, a mistyped code, a third player. → [test log](verification/test-log.md#not-yet-verified)
- **Pin a quick-match region** with a `PHOTON_REGION` secret or variable fed into `-photonRegion`, so quick-matchers in different best regions still meet. → [quick match](netcode/sessions.md#quick-match)
- **Measure real bandwidth and CCU** during a co-op session.

## Pipeline

- **Deploys:**
  - publish `main` to the Pages root and branches to `/<branch>/` previews, with `destination_dir` + `keep_files` or `actions/deploy-pages` with environment rules
  - make the concurrency group per-branch first
- **Tests:**
  - add `game-ci/unity-test-runner` (EditMode tests for pure logic) ahead of the build
  - run [`coop.mjs`](verification/tools/coop.mjs) against the fresh deploy *in CI*. GitHub-hosted runners can open WebSockets, unlike the cloud sandbox.
- **A pre-build check without Unity:** the [type-check and weave](verification/README.md) could run in a fast job before the long Unity build. It needs a cached copy of the extracted editor DLLs.
- **Supply chain:** pin third-party actions to commit SHAs, and revisit `unity-builder` v6 after a few point releases.
- **More platforms:** the same workflow becomes a `targetPlatform` matrix for desktop and Android.
- **Size:** probably leave it. Brotli plus `decompressionFallback` would save perhaps 15–20% of the 10.4 MB at the cost of JavaScript decompression.

## Code and content

The refactor candidates are listed, with the roadmap items each one unlocks, in [refactor-candidates.md](roadmap/refactor-candidates.md). The frameworks check recommended two adoptions:
- **`ObjectPool<T>`** for projectiles, particles and popups.
- **uGUI or UI Toolkit** to replace the IMGUI HUD and menus.

Later, and not blockers:
- URP and the Pixel Perfect Camera, before the Built-in pipeline is removed.
- The Aseprite importer or LDtk, once real art or levels arrive.

## Gameplay gaps

- No touch controls: it's keyboard or gamepad only.
- The automated play test covers the first seconds only. The later level and the tank boss have only had human playthroughs.
- Co-op has no reconnect after a drop, and no message when a partner's tab is hidden ([hidden tabs](netcode/sessions.md#hidden-tabs)).
