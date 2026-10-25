# Verification without Unity

There's no Unity install or licence in a cloud session. One command does most of what CI does before a build, in about 30 seconds.

**Read this when** you're about to push C# or networking changes, upgrading Unity or an SDK, or deciding how to test something.

Up: [Research index](../README.md)

## Run `tools/check.sh`

```sh
tools/check.sh           # everything below
tools/check.sh compile   # only the type-check
tools/check.sh docs      # only the docs link check (no kit needed)
tools/check.sh ownership # only the ownership check (no kit needed)
```

It runs, in order:
1. **The type-check.** Every assembly (Core, its tests, Net, the game and the editor code) is compiled as Unity compiles it for WebGL ([type-check.md](type-check.md)).
2. **Fusion's IL weaver** over the netcode ([fusion-weaver.md](fusion-weaver.md)).
3. **The EditMode tests** ([edit-mode-tests.md](edit-mode-tests.md)): 795 at the time of writing, 794 passing and 1 ignored.
4. **The scene check**, **the docs link check** and **the ownership check** (every file belongs to exactly one agent domain, `.claude/hierarchy/OWNERSHIP.txt`).

A full run takes about 30 seconds once the kit is cached. The first run downloads the Unity kit for the project's Unity version (the vendor repository's *Unity kit* workflow makes it) and the Photon SDK, both from the private vendor repository. That takes about a minute. After a Unity upgrade, run that workflow first, giving it the new version (left empty, it makes the main line's).

## What still needs CI

| Check | Why not here | Where it runs |
|---|---|---|
| PlayMode tests (`Assets/Tests/Gameplay`) | They need a real `World` inside Unity | The build workflow |
| Unity's import itself: `.meta` files, serialisation, IL2CPP and stripping, the jslib bridge | Only Unity does the import | The build |
| The WebGL build | Needs the editor | The build |
| Touch play (`tools/touch.mjs`) | Needs the build | The build workflow |
| Kit demos (`tools/demos.mjs`) | Needs the build | The main line's builds; a preview's only with a `CI: demos` commit line |
| Co-op tests (`tools/coop.mjs`: code, boss, scene, dropin, handover, watch, debrief) | Needs Photon's WebSockets, which cloud sessions can't open | The build workflow, one job per scenario |

A preview is up about 12 minutes after a push, and the live game about 18 to 20. Read the run and fix it if red ([workflow](../build-pipeline/workflow.md)).

## The other docs

- [type-check.md](type-check.md): how the compile works and why. Read it when a compile error needs explaining, or Unity is upgraded.
- [fusion-weaver.md](fusion-weaver.md): how the weaver is run outside Unity. Read it after changing a networked type or an RPC.
- [edit-mode-tests.md](edit-mode-tests.md): how to write a test for Core, and how CI runs them.
- [browser-tests.md](browser-tests.md): the Playwright smoke, touch and co-op tests, and the cloud sandbox's network limits.
- [tools/](tools/README.md): the scripts `check.sh` calls, and the odd ones.
