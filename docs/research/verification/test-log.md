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
| — | Cardmaster's hosting and sign-in tests, with the change | Its CI, once the branch is pushed | Not run yet: the commit is waiting on [owner 2c](../todo.md) |

## Not yet verified

These were implemented, but none has been confirmed yet. Host migration was on this list until the CI co-op test confirmed it, and it's now checked on every build ([architecture](../netcode/architecture.md#host-migration)).
- **Quick match** between two players.
- **The *Nobody is hosting* path** after a mistyped code.
- **The *Game is full* path** for a third player.
- **Bandwidth and CCU use** under real play.

`coop.mjs` in `quick` mode covers quick match. Run it from a machine with normal network access, or follow the [manual steps](../netcode/sessions.md#testing-by-hand), and add the results here.
