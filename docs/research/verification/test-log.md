# Test log

What was tested, how, and the result. All times are UTC on 2026-10-14. CI run IDs link to `https://github.com/jhiggins-tech/pooket-slug/actions/runs/<id>`.

**Read this when** you need to know whether something has actually been verified, or by what.

Up: [Verification](README.md)

## Part 1: GameCI → WebGL → GitHub Pages (solo game)

| When | What | How | Result |
|---|---|---|---|
| ~04:40 | Game and editor scripts compile | [Type-check](type-check.md) against 6000.3.26f1's DLLs, with both input backends | 0 errors, 0 warnings |
| 04:44–04:51 | First CI build and deploy | Run #1, 38512931962 (cold) | ✅ 6 min 41 s, first try |
| 04:52 | Run #2, 38513669010 | — | Cancelled by the concurrency group when #3 was pushed |
| 04:53–04:59 | Build with the Library cache restored | Run #3, 38513734583 | ✅ 5 min 33 s |
| after each deploy | The page loads and plays | [smoke.mjs](tools/smoke.mjs) in headless Chromium | ✅ Engine boots, input works, no console errors. Found blank `∞`/`→` glyphs, which were fixed |
| ~05:10 | Played by a person | The repo owner | ✅ "Love it" |

## Part 2: Photon Fusion co-op

| When | What | How | Result |
|---|---|---|---|
| ~07:05 | The Photon SDK compiles under our module set | `compile_unity.py` `sdk` stage | ✅ after adding the `ai`, `animation`, `imageconversion`, `physics`, `physics2d` and `unitywebrequest` modules and mono-cecil 1.11.6 |
| 07:09 and 07:36 | CI fetches the SDK | Run 38526546296, attempts 1 and 2 | ❌ `VENDOR_TOKEN is not set`: the secret had been created as `VENDOR_PAT`. Fixed in `0067cc5` |
| ~07:45 | Co-op code compiles and weaves | `compile_unity.py` `game` + [weave harness](fusion-weaver.md) | ✅ no diagnostics; `PlayerNet` 8 words, `WorldNet` 151, 7 RPC invokers |
| 07:53–07:58 | — | Runs 38531363579 and 38531447220 | Cancelled when newer commits were pushed |
| 07:58–08:06 | First CI build with the SDK and co-op code | Run 38531767207 | ✅ SDK fetched and verified; weaver compiled in the same pass; 0 errors |
| 08:08 | Deployed build still plays solo | smoke.mjs | ✅ The title shows *Online co-op is off (no Photon AppId)* as designed |
| 08:08–08:17 | Workflow without the warm-up step | Run 38533066347, attempt 1 | ✅ (`-photonAppId` empty, so co-op off) |
| 08:23–08:30 | Build with `PHOTON_APP_ID` | Run 38533066347, attempt 2 | ✅ `Photon AppId baked in` |
| 08:31 | Two-browser co-op test | [coop.mjs](../../../tools/coop.mjs) from the cloud sandbox | ⚠️ Blocked by the sandbox proxy (WebSocket upgrade → 502). The online menu, template registration, runner start-up and graceful failure were all verified ([detail](browser-tests.md#the-sandboxs-limits)) |
| ~08:45 | **Live two-player session** on build `0.2.0+791a155` | The repo owner, on a normal network | ✅ "tested online, v happy with that!" |
| 13:44–13:45 | **Two-player co-op test in CI**, against the build of `cfee223` | Run 38573051144, job *Two-player co-op test*: [coop.mjs](../../../tools/coop.mjs) in two headless Chromium instances on GitHub's runner, code mode | ✅ All five checks, in 55 s: A hosted `HCNW-AU`, B joined with the invite link, A saw B join, both played with no errors or disconnects, and B took over the shared world when A left |

## Part 3: Sign-in

All tests ran against Cardmaster's repository at `762ca94`, in the cloud sandbox, with Node 24.21 and Java 21.

| When | What | How | Result |
|---|---|---|---|
| ~10:39 | Cardmaster's full Firebase suite, unchanged | `npm run test:firebase`, which starts the emulators through the Firebase CLI | ❌ `Failed to load initial Database Emulator rules`: the CLI sent its call to the local emulator through the sandbox proxy ([why](../accounts/database-rules.md#running-the-tests-in-the-cloud-sandbox)) |
| ~10:40 | Cardmaster's rules tests, unchanged | Database emulator 4.11.2 started directly, as in [rules-test.sh](tools/rules-test.sh) | ✅ 17 of 17 |
| ~10:42 | With the drafted `pooketSlug` section and its tests | The same | ✅ 24 of 24 |
| ~10:43, rerun ~10:48 | Do the new tests catch weakened rules? | The rules weakened 12 ways, one at a time ([list](../accounts/database-rules.md#tests)) | ✅ 12 of 12 caught: each run failed, with a named test. The first pass's exit codes were unreliable (the script's cleanup masked them), so it was rerun with the fixed script, checking both the exit code and the named failure |
| ~10:44 | Cardmaster's typecheck and unit tests, with the change | `npm run typecheck`, `npm run test:unit` | ✅ no type errors; 57 of 57 |
| — | Cardmaster's hosting and sign-in tests, with the change | Its CI, once the branch is pushed | Not run then: the commit waited on [owner 2c](../todo.md) |
| ~21:53 | The draft, extended to the planned record and the friends board, with a save number | The same, on Node 22.22. 13 tests for `pooketSlug` in place of the draft's 7 | ✅ 30 of 30: Cardmaster's 17, and 9 for the record and 4 for the board. One new test was wrong at first (it expected an overlong player id to be refused; the rules bound mission ids) and was fixed |
| 21:54–21:59 | Do the new tests catch weakened rules? | The new rules weakened 32 ways, one at a time ([list](../accounts/database-rules.md#tests)) | ✅ 32 of 32 caught, each by a named test. Three needed a second pass, after their search patterns had also matched Cardmaster's own rules |
| 21:59–22:01 | Cardmaster's CI on the committed rules (`121c939`, branch `claude/gallant-keller-3qzp97`), with the owner's go-ahead | Run 38644449649 | ✅ Build, and the rules, hosting and sign-in tests passed; the deploy job was skipped, as it only runs on `main` |
| 22:29–22:31 | The pull request, [jhiggins-tech/cardmaster#11](https://github.com/jhiggins-tech/cardmaster/pull/11), and its merge by the owner | Run 38647981617 on the pull request, then run 38648058033 on `main` | ✅ Both passed. On `main`, *Deploy the database rules and the PWA* succeeded: the rules are live |
| ~23:06 | The sign-in code: page script, bridge, cloud store, build config | `compile_unity.py game` for each input backend (no SDK; the netcode is unchanged) | ✅ Core 17 files, game 25, editor 1: no errors or warnings |
| ~23:10 | The page's sign-in and cloud saves, against Cardmaster's live rules file in the database emulator | [`auth-page.mjs`](tools/auth-page.mjs): headless Chromium, a stand-in for the game, and fake Google and Auth servers | ✅ 22 of 22: first save, a second device merging, a device that was behind merging after a refusal, a reload, signing out, a second tab, storage keys, and another player refused |
| 23:14–23:30 | The first sign-in build (`6f2cef4`) | CI run 38652840641 | ✅ Built, deployed, co-op test passed. But `[PooketSlug] Sign-in: off`: the three values were repository **secrets**, not variables, so `vars.*` was empty |
| 23:25–23:45 | Reading them from variables or secrets (`23107e1`) | CI run 38654218370 | ✅ Deployed `pooket-config.js` has all three values. The co-op test first failed only its crate check (no pickup handed out; everything else passed), then passed all six on a re-run of the same build. Treat the crate check as possibly flaky |
| ~23:12 | Does it catch broken page code? | Three breakages, one at a time: no retry after a refusal, the account state sent after the profile, an unprefixed storage key | ✅ 3 of 3 caught (4, 3 and 1 checks failed) |

## Part 4: Refactor

The [refactor plan](../roadmap/refactor-plan.md)'s steps, as they land.

| When | What | How | Result |
|---|---|---|---|
| 13:48–14:00 | Step 1: log channels, tuning in one place, seeded randomness (`243f025`) | Local compile and weave, then CI run 38574958671 | ✅ Built and deployed. The co-op test passed all five checks, logging the new `[PooketSlug/Net]` lines |
| ~14:03 | Step 2: the `PooketSlug.Core` assembly and its tests | `compile_unity.py` with assembly definitions, with and without the SDK, then the weave harness | ✅ Core 8 files, tests 7, game 18, editor 1: no errors or warnings. The weaver's output is unchanged: `PlayerNet` 8 words, `WorldNet` 151 |
| ~14:03 | The EditMode tests, locally | [`run_tests.py`](tools/run_tests.py): NUnitLite 3.14 on the editor's .NET 6 runtime | ✅ 101 of 101 in 0.1 s |
| ~14:05–14:12 | Do the tests catch bugs? | Core broken 11 ways, one at a time ([list](edit-mode-tests.md#do-the-tests-catch-bugs)) | ✅ 11 of 11 caught, each by the tests you'd expect |
| 14:10–14:12 | Step 2 in CI (`367a378`) | Run 38578138923 | ❌ The test step stopped before Unity started. `unity-test-runner@v4` (now v4.4.0, a wrapper around `game-ci/cli`) passed `--no-coverageEnabled`, and CLI v0.1.72 rejected it. Fixed by pinning v4.3.2 ([why](../build-pipeline/workflow.md#action-versions)) |
| 14:16–14:27 | Step 2 in CI, with the runner pinned (`74d5b7b`) | Run 38579082894 | ✅ 101 of 101 EditMode tests in Unity 6000.3.26f1 (Test Framework 1.6.0, NUnit 3.5): 36 s inside Unity, 3 min 5 s for the step with the image pull the build used to do. Then the build, the deploy, and the co-op test, all passed. The build is still 33.8 MB |
| 14:27–14:39 | Step 3: pooling (`6eae099`) | Run 38580730821 | ✅ Built, deployed, and passed the co-op test |
| ~14:37 | Does the deployed build reuse its objects? | A headless browser played the live page with `?log=game:debug`, holding fire and throwing grenades | ✅ `Projectile pool: 64 used, 6 made` and `Effect pool: 64 used, 3 made`, no errors. Software rendering ran at 19 FPS, so only 17 s of game time passed |
| 14:39–14:49 | Step 5: `World` split into systems (`5555b2e`) | Run 38582424832 | ✅ Built, deployed, co-op test passed |
| 14:50–15:01 | Step 6: content as data (`240cfe9`, `095077d`) | Run 38584035942 | ✅ Built, deployed, co-op test passed. Shots now carry weapon ids, and the snapshot enemy ids |
| 15:07–15:17 | Step 7: typed network events (`0a3832f`) | Run 38586628482 | ✅ Built, deployed, co-op test passed |
| 15:18–15:36 | Step 8: the netcode assembly (`0607a04`) | Run 38588224449 | ✅ The build log says `co-op code compiled: True`, and Unity's weaver ran over `PooketSlug.Net`. The first build with the new assembly took 16 minutes, against about 8 |
| 15:36–15:46 | Step 9: dynamic network ids (`ea92974`) | Run 38590865323 | ✅ Built, deployed, co-op test passed |
| 15:47–15:56 | Step 12: `NetSession` split in two (`c78c601`) | Run 38592395346 | ✅ Built, deployed, co-op test passed |
| 15:57–16:07 | Step 11: each player's soldier replicated (`f8c154e`) | Run 38593864316 | ✅ Built, deployed, co-op test passed |
| 16:07–16:24 | Step 10: the replicated pickup table (`8988cea`) | Run 38595398658 | ❌ Built and deployed (140 of 140 tests), but the co-op test failed twice: Photon refused A's connection with `InvalidAuthentication`, before any of the step's code runs |
| 16:24–16:38 | Was it step 10? | The AppId baked into both builds compared by hash; step 11 rebuilt and run again; then step 10's co-op test once more | ✅ The same AppId; step 11 passed at 16:35, and step 10's own build passed at 16:38. A brief rejection on Photon's side, about 16:18–16:24. No pickups were given out: the soldiers reach the first crate's bridge in 13 s but don't climb onto it |
| 16:39–16:52 | Step 13: input sources and the touch pad (`e96b9ab`) | Run 38599941742 | ✅ Built, deployed, co-op test passed |
| ~16:53 | Does the pad work on a phone? | [`touch.mjs`](tools/touch.mjs): the live page on an emulated Pixel 7 held sideways, two thumbs through Chromium's DevTools protocol | ✅ Touch hints on the title; a tap starts; with the stick pushed right and FIRE held, the soldier runs and fires; II pauses. The page now gives the game a 16:9 canvas (548×308); the old page squeezed it to 831×208 |
| 16:53–17:11 | Step 14a: the HUD in uGUI (`7264c9c`) | Run 38601925207, then `touch.mjs` on the live page | ✅ Built, deployed, co-op test passed; the pad, now drawn by uGUI, passed the touch test. The first build with uGUI took 16 minutes |
| ~17:12 | What did uGUI cost? | The files of step 10's build artifact, against the live build's, raw and as Pages serves them (gzip) | ⚠️ The wasm grew from 28.56 MB to 42.12 MB, and the data from 6.51 MB to 8.74 MB: about 10.2 MB to 13.95 MB on the wire. Put down at first to Unity's default stripping level, which ships package assemblies whole; `25d4276` tried `Low`, and the real cause turned up at 18:09, below |
| 17:12–17:28 | Step 15: the profile and its store (`f79bf88`) | Run 38604613150 | ✅ Built, deployed, co-op test passed; 171 of 171 EditMode tests |
| 17:28–17:39 | Step 14b: the menus in uGUI, and co-op shown once it's earned (`8a748f9`) | Run 38607000229. The co-op test now opens the page with `?unlock=coop`, since a fresh browser hasn't beaten Mission one | ✅ Built, deployed, co-op test passed |
| 17:39–17:51 | Step 16: missions as data (`6baac2b`) | Run 38608549578 | ✅ Built, deployed, co-op test passed; 175 of 175 EditMode tests |
| 17:40–17:46 | The touch test on step 14b | `touch.mjs` nine times on the live page, checking FIRE's pixels in each screenshot | ⚠️ In one of the nine, FIRE never lit while the stick worked; the other eight were fine. Two causes were possible, a missed touch-down or a thumb landing left of the middle, and `74fc4e0` (below) handles both |
| ~17:52 | Co-op stays hidden until it's earned, on the live build | A headless browser with fresh storage: an invite link, then `?unlock=coop` | ✅ The invite link starts Mission 1 with *Co-op opens once you've beaten this mission*. With `?unlock=coop` the title shows CO-OP, with a NEW! tag |
| 17:52–18:09 | The follow-ups: managed stripping at `Low` (`25d4276`) and the pad fix (`74fc4e0`) | Run 38610328624. The build log, then the live page's file sizes | ⚠️ 177 of 177 tests; the log says `Managed stripping: Low; Photon SDK kept whole: True`; built, deployed, co-op test passed. But `Low` saved 24 KB: the wasm went from 42,116,218 bytes to 42,092,152, and the data file by 0.6 KB. Reverted in `4a8a12b`, below |
| 17:58–18:01 | Do the tests catch bugs in the refactor's later code? | Core at `74fc4e0` broken 21 ways, one at a time: the touch pad, input, the profile and the missions ([list](edit-mode-tests.md#do-the-tests-catch-bugs)) | ✅ 21 of 21 caught |
| 18:09–18:20 | Why didn't stripping help? | The build artifacts of steps 13 and 14a and of the follow-up, unpacked: the assemblies each ships, and the types each kept in its IL2CPP metadata | ✅ UI Toolkit: 0 types at step 13, 1,654 with uGUI. The Input System brings in UI Toolkit's module, so uGUI's event system calls into it, and stripping can't remove what's reachable. uGUI itself is 205 types, whole at both levels; TextMeshPro is stripped entirely ([numbers](../build-pipeline/numbers.md#what-ugui-costs)) |
| 18:14–18:17 | The pad fix on the live build (`74fc4e0`) | `touch.mjs` six times, checking FIRE's pixels in each | ✅ FIRE lit in six runs of six, with no errors; the stick ran the soldier, and II paused |
| 18:17–18:37 | The revert to `Minimal` (`4a8a12b`) | Run 38613952856, then the live page's sizes and `touch.mjs` twice | ✅ 177 of 177 tests; built, deployed, and the co-op test passed all five checks in 57 s (no pickups given out). The wasm is 42,145,618 bytes and the page 14.05 MB over the wire, as before `Low`. FIRE lit in both touch runs |

## Part 5: After the refactor

The owner's next picks, from 2026-10-14: the page in short windows, a crate in the co-op test, and the AU region.

| When | What | How | Result |
|---|---|---|---|
| ~21:47 | The page in short desktop windows, before and after the fix | The live page, then the new template rendered locally around the live build, each measured at ten window sizes, desktop and phone; then the game started in a 1280×600 window | ✅ Before: squashed when short, 960×448 in a 1280×600 window and 960×268 at 1366×420. After: 16:9 at all ten sizes, 782×440 at 1280×600 with nothing scrolling. Only a 420-px-tall window scrolls, by 59 px, with the game fully in view |
| 21:49–22:01 | The page keeps 16:9 in short windows (`298a8d1`) | Run 38643150666, then the live page measured again, and `touch.mjs` once | ✅ Built, deployed, co-op test passed; the live page matches the local measurements; FIRE lit on the emulated phone |
| 22:02–22:15 | The co-op test needs a crate handed out (`9cf50f0`): `?test=crate` puts one just ahead of the start | Run 38644817688, then `?test=crate` in a solo game on the live page | ✅ All six checks, the new one included: `Pickup 33 (Heavy machine gun crate) goes to player 1` on A, 9 s after B joined. It's the first time the shared pickup table (refactor step 10) handed out a pickup between two browsers. Solo, running right picks the crate up: *Heavy machine gun!*, HMG 200 |
| 22:15–22:27 | One region (Australia) and four-letter room codes (`ac2cc75`) | Locally: compiled with and without the SDK, 174 of 174 EditMode tests, the weaver. Then run 38646408096 | ✅ All six checks: A logged `Online in region au: room PPLG`, B joined with `?room=PPLG`, A's world handed out the crate, and B took over when A left. A's screen shows *Room code PPLG* and a link ending `?room=PPLG`; ping from GitHub's US runner to Australia, 164 ms |

## Not yet verified

These were implemented, but none has been confirmed yet. Host migration was on this list until the CI co-op test confirmed it, and it's now checked on every build ([architecture](../netcode/architecture.md#host-migration)).
- **Quick match** between two players.
- **The *Nobody is hosting* path** after a mistyped code.
- **The *Game is full* path** for a third player.
- **Bandwidth and CCU use** under real play.

`coop.mjs` in `quick` mode covers quick match. Run it from a machine with normal network access, or follow the [manual steps](../netcode/sessions.md#testing-by-hand), and add the results here.
