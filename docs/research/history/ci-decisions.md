# CI decisions and what went wrong

This is a record, not instructions. The live version is [the workflow](../build-pipeline/workflow.md).

Up: [Research index](../README.md)

## Action versions

- **`unity-builder` v4 or v6:** v6 (August 2026) is a thin wrapper around `game-ci/cli` on Node 24. v4.8.x is the long-standing implementation on Node 20 and still gets backports. We use v4; it logs a harmless "Node.js 20 is deprecated ... forced to run on Node.js 24" warning.
- **`unity-test-runner` v4.3.2:** the last release that runs Unity itself. v4.4.0, which the `v4` tag now points to, wraps the `game-ci/cli` binary, fetched at `latest`. Our first test run used `v4` with `coverageEnabled: false`: the action passed `--no-coverageEnabled`, CLI v0.1.72 rejected it as an unknown argument, and the step failed before Unity started (run 38578138923).

## The kit demos' recording

The first cut recorded the whole page and cut it at logged times, which drifted by seconds. The job now records the game's own canvas from each demo's first frame to its last. Added 2026-10-16.

## Concurrency and disk space

- Until 16 Oct the concurrency group was global, with `cancel-in-progress: true`: several threads pushing to one branch kept cancelling each other and nothing deployed for over an hour.
- Until 17 Oct, `jlumbroso/free-disk-space@v1.3.1` freed disk by deleting Android, .NET, Haskell and pre-pulled Docker images, which took 51 to 145 s. The `spike-ci-disk` run showed the runner's second disk is enough.
