# Verification without Unity

There's no Unity install or license outside CI, and a CI round trip costs 6–8 minutes. These techniques catch most problems before pushing, which is why the first CI run of the game went green and the Fusion integration built first time.

**Read this when** you're about to push C# or networking changes, upgrading Unity or an SDK, or deciding how to test something.

Up: [Research index](../AGENTS.md)

| Technique | Catches | Cost | Doc |
|---|---|---|---|
| Type-check against the GameCI image's own Unity DLLs | Compile errors in game and editor scripts; API misuse; C# 9 features Unity doesn't accept; **missing engine modules** | One-off ~7 GB stream; then seconds per compile | [type-check.md](type-check.md) |
| Run Fusion's IL weaver outside Unity | Weaver errors (networked property and RPC rules), and confirms what gets generated | Seconds, after the type-check | [fusion-weaver.md](fusion-weaver.md) |
| Smoke-test the deployed build in headless Chromium | The template renders, the engine boots, input reaches the game, console errors, visual glitches such as missing font glyphs | ~1 min per deploy | [browser-tests.md](browser-tests.md) |
| EditMode tests for the pure logic in `PooketSlug.Core` | Regressions in tile physics, level parsing, pose packing, room codes, randomness, address parsing and logging | Under a second locally; CI runs them before each build | [edit-mode-tests.md](edit-mode-tests.md) |
| Two-browser co-op test | Joining, shared world, host migration | A few minutes; **needs WebSocket egress**, so it runs in CI after each deploy | [browser-tests.md](browser-tests.md#two-browser-co-op-test) |
| Read CI logs for the `[PooketSlug]` diagnostics line | SDK present, weaver compiled, defines, AppId baked | Seconds | [workflow.md](../build-pipeline/workflow.md#reading-ci-logs) |

Recommended order before pushing a C# change:
1. Type-check both input backends.
2. Type-check with the SDK, then weave.
3. Run the EditMode tests.
4. Push.
5. Read the test summary and the `[PooketSlug]` lines.
6. Smoke-test the deploy.
7. Read the co-op test's summary in the CI run. Co-op paths it doesn't drive need a person on a normal network.

- [type-check.md](type-check.md): extracting Unity's compiler and DLLs, compiling like the editor, emulating disabled modules, compiling the Input System and the Photon SDK.
- [fusion-weaver.md](fusion-weaver.md): driving Fusion's `ILPostProcessor` from a small .NET harness, and inspecting the woven output.
- [edit-mode-tests.md](edit-mode-tests.md): the Core assembly's NUnit tests, how CI and you run them, and the bugs they caught on purpose.
- [browser-tests.md](browser-tests.md): the Playwright smoke and co-op tests, software WebGL, and the cloud sandbox's network limits.
- [tools/](tools/README.md): the scripts.
