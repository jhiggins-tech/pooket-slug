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

## Session and network costs

- **Photon free plan:** 20 CCU, i.e. 10 two-player sessions at once, plus 3 GB of traffic per CCU per month ([choosing a stack](../netcode/choosing-a-stack.md#free-tier-and-licensing)).
- **Replicated state:**
  - `PlayerNet` is 8 words (32 bytes) per player.
  - `WorldNet` is 151 words: 24 enemy slots of 6 words each, plus 7 words of world state.
  - Fusion sends only the words that changed ([architecture](../netcode/architecture.md#replicated-state)).
  - Actual bandwidth hasn't been measured yet. Before choosing Fusion we estimated 2–4 KB/s per client.
