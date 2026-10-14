# Numbers: build times and download sizes

Measured on GitHub-hosted `ubuntu-latest` runners and on the live page, 2026-10-14. Run IDs are given so each figure can be checked in the Actions tab.

**Read this when** you need a baseline before changing the pipeline, adding a package or SDK, or estimating CI cost.

Up: [Build pipeline](README.md)

## Build times

The "unity-builder step" includes pulling the ~7 GB editor image, activating the license and building. "Unity BuildPlayer" is the build report's own `Duration`.

### Before Fusion (solo game)

| | Run #1 (cold) | Run #3, 38513734583 (Library cache restored) |
|---|---|---|
| Whole job | 6 min 41 s | 5 min 33 s |
| unity-builder step | 330 s | 259 s |
| Free disk space | 58 s | 60 s |
| Upload artifact + deploy | ~5 s | ~6 s |

- The Library cache was **83 MB** compressed and saved about **70 s** per build.
- Run #2 was cancelled on purpose, by the concurrency group.

### With the Fusion SDK

| | 38531767207 | 38533066347, attempt 1 | 38533066347, attempt 2 |
|---|---|---|---|
| Commit | `b038c6f` | `791a155` | `791a155` |
| What was different | First build with the SDK; Library restored from a pre-Fusion cache | Warm Library | Re-run after adding `PHOTON_APP_ID` |
| Whole job | 8 min 10 s | 8 min 14 s | **6 min 15 s** |
| Free disk space | 68 s | 145 s | 51 s |
| Fetch Photon SDK | 1 s | 2 s | 1 s |
| Restore Library cache | 2 s | 3 s | 5 s |
| unity-builder step | 6 min 43 s | 5 min 30 s | 5 min 01 s |
| Unity BuildPlayer | 3 min 28 s | 1 min 51 s | 1 min 55 s |
| Upload artifact + deploy | 8 s | 8 s | 8 s |

Takeaways:
- **The SDK adds about 1 minute per build.** That's the extra import, compiling five more assemblies, IL weaving, and a larger IL2CPP build. The first build after adding it paid about 3 minutes more, because the cached Library predated the SDK.
- **"Free disk space" varies a lot,** from 51 to 145 s. It's the noisiest step.
- **Fetching and verifying the 6.4 MB SDK package is negligible,** at 1–2 s.

## Download size

What a browser fetches, measured from the live page with and without `Accept-Encoding: gzip`. Pages gzips responses on the fly ([why](github-pages.md#compression)). Sizes are in MB (10⁶ bytes); Unity's build report uses MiB, so its "33.8 mb" is the same 35.4 MB.

| File | Before Fusion: raw | Before Fusion: gzipped | With Fusion: raw | With Fusion: gzipped |
|---|---|---|---|---|
| `WebGL.wasm` | 18.9 MB | 5.8 MB | 28.43 MB | 8.30 MB |
| `WebGL.data` | 4.4 MB | 1.3 MB | 6.49 MB | 2.00 MB |
| `WebGL.framework.js` | 0.4 MB | 0.09 MB | 0.45 MB | 0.09 MB |
| `WebGL.loader.js` + `index.html` | ~32 KB | — | 32 KB | 11 KB |
| **Total** | **23.8 MB** | **7.2 MB** | **35.4 MB** | **10.4 MB** |

- **Fusion and Photon add 11.6 MB raw and about 3.2 MB over the wire.** Most of it is code in the `.wasm`.
- **Load time:** the page loaded in about 1.8 s before Fusion and about 3.5 s after, in headless Chromium in the cloud sandbox (cold cache). Those numbers say little about real devices.
- **Frame rate:** the FPS counter reads 17–29 in headless Chromium, which renders WebGL in software (SwiftShader). That isn't a real-GPU number either.

### What uGUI costs

uGUI replaced IMGUI in refactor step 14a. Steps 13 and 14a compared, from their build artifacts and the live page:

| File | Step 13: raw | Step 13: gzipped | With uGUI: raw | With uGUI: gzipped |
|---|---|---|---|---|
| `WebGL.wasm` | 28.62 MB | 8.22 MB | 42.12 MB | 11.09 MB |
| `WebGL.data` | 6.52 MB | 1.99 MB | 8.74 MB | 2.86 MB |
| **Total**, with the framework, loader and page | **35.6 MB** | **10.3 MB** | **51.4 MB** | **14.1 MB** |

Step 13's gzipped sizes are `gzip -6` of its artifact; for step 14a, Pages' own gzip came out about 1% bigger than that. So uGUI added about **3.7 MB over the wire, 36%**.

**Where it comes from.** Types that survived stripping, counted in each build's IL2CPP metadata (`global-metadata.dat`, inside `WebGL.data`):

| Assembly | Step 13 | With uGUI | What it is |
|---|---|---|---|
| `UnityEngine.UIElementsModule` | 0 | 1,654 | UI Toolkit, which the game doesn't use |
| `UnityEngine.PropertiesModule`, `UnityEngine.HierarchyCoreModule` | 58 | 214 | What UI Toolkit needs |
| `UnityEngine.UI` | — | 205 | uGUI: all of it, since `Minimal` copies package assemblies whole |
| `Fusion.Unity` | 77 | 119 | Fusion's statistics overlay, compiled in when uGUI is present (`FUSION_ENABLE_UGUI`) |
| `Unity.TextMeshPro` | — | 0 | Part of uGUI 2.0; stripped entirely |
| All | 5,999 types, 36,422 methods | 8,217 types, 53,471 methods | |

**Why UI Toolkit is there:**
- The Input System 1.20.0 depends on the UI Toolkit module, `com.unity.modules.uielements`.
- In a project with that module, uGUI compiles its UI Toolkit bridge (`PACKAGE_UITOOLKIT`), and `EventSystem.OnEnable`, `Start` and `Update` call it. The menus' event system therefore reaches UI Toolkit's runtime, and no stripping level removes code that's reachable.
- Before uGUI nothing reached it, so the linker dropped all of it.
- **`Low` stripping didn't help.** It was tried on 2026-10-14 (`25d4276`, reverted): 24 KB less wasm, and uGUI still whole.

**Ways to win it back, none tried:**
- Brotli with `decompressionFallback`, perhaps 15–20% less to download ([compression](github-pages.md#compression)).
- Handle taps, clicks and the room-code entry ourselves instead of through uGUI's event system, then strip at `Medium`. That should drop the event system and UI Toolkit with it. It costs uGUI's ready-made sliders, scroll views and text fields, which settings and a room browser would want.

## Session and network costs

- **Photon free plan:** 20 CCU, i.e. 10 two-player sessions at once, plus 3 GB of traffic per CCU per month ([choosing a stack](../netcode/choosing-a-stack.md#free-tier-and-licensing)).
- **Replicated state:**
  - `PlayerNet` is 9 words (36 bytes) per player.
  - `WorldNet` is 215 words: 24 enemy slots of 6 words each, 16 pickup slots of 4 words each, plus 7 words of world state.
  - Fusion sends only the words that changed ([architecture](../netcode/architecture.md#replicated-state)).
  - Actual bandwidth hasn't been measured yet. Before choosing Fusion we estimated 2–4 KB/s per client.
