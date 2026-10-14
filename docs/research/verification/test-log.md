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
| 08:31 | Two-browser co-op test | [coop.mjs](tools/coop.mjs) from the cloud sandbox | ⚠️ Blocked by the sandbox proxy (WebSocket upgrade → 502). The online menu, template registration, runner start-up and graceful failure were all verified ([detail](browser-tests.md#the-sandboxs-limits)) |
| ~08:45 | **Live two-player session** on build `0.2.0+791a155` | The repo owner, on a normal network | ✅ "tested online, v happy with that!" |

## Not yet verified

These were implemented, and some are covered by `coop.mjs`, but none has been explicitly confirmed:
- **Host migration:** the master client leaves and the other client takes over the world ([architecture](../netcode/architecture.md#host-migration)).
- **Quick match** between two players.
- **The *Nobody is hosting* path** after a mistyped code.
- **The *Game is full* path** for a third player.
- **Bandwidth and CCU use** under real play.

Run `coop.mjs` from a machine with normal network access, or follow the [manual steps](../netcode/sessions.md#testing-by-hand), and add the results here.
