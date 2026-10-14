# Next steps

What we'd do if this went further. The active checklist is [todo.md](todo.md), and feature choices live in the [roadmap](roadmap/README.md); this page covers process, infrastructure and gaps.

**Read this when** you're picking up work after a pause.

Up: [Research index](AGENTS.md)

## Queued and open items

The working checklist, including who does what and what's blocking what, is in [todo.md](todo.md). This page keeps the longer-term ideas.

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
