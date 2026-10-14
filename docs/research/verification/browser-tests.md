# Browser tests

Testing the real deployed build in headless Chromium with Playwright: a single-player smoke test, a two-browser co-op test, and what the cloud sandbox can't test.

**Read this when** a deploy finished and you want to know it works, or you're about to test co-op.

Up: [Verification](README.md)

## Running Unity WebGL headless

- **Chromium needs flags to run WebGL 2 in software:** `--use-gl=angle --use-angle=swiftshader --enable-unsafe-swiftshader`. Add `--autoplay-policy=no-user-gesture-required` so audio starts without a click.
- **Expect 17–29 FPS** on the game's own counter in software rendering. That says nothing about real GPUs.
- **Wait for the template's loader to hide** (`#loader` → `display: none`) before interacting. Then click the canvas, so it has keyboard focus.
- **Keyboard input works** through `page.keyboard`, including holding keys with `down`/`up`. The game reads it through the Input System.
- **Collect `console` and `pageerror` events.** Unity's `Debug.Log` arrives as `console.log`, so the game's `[PooketSlug]` lines can be asserted on.

## Smoke test

[`smoke.mjs`](tools/smoke.mjs) `<url> <outdir>` does the following:
1. Loads the page and screenshots the title.
2. Presses Enter.
3. Runs right while firing and jumping, then throws a grenade.
4. Takes screenshots along the way and prints the last console lines.

- **Every deploy of the spike was checked this way.**
- **It caught the IMGUI font's missing `∞` and `→` glyphs.** No compile check could have.
- **It confirmed the co-op build still plays solo,** with the *Online co-op* menu line present.

## Two-browser co-op test

[`coop.mjs`](../../../tools/coop.mjs) `<url> <outdir> [code|quick]` runs **two separate Chromium instances**, not two tabs. **CI runs it on every build** ([the job](../build-pipeline/workflow.md#the-two-player-co-op-test-job)). Tabs could be throttled ([hidden tabs](../netcode/sessions.md#hidden-tabs)).

1. A loads the page with `?unlock=coop&test=crate`. `unlock` is needed because a new browser hides co-op until Mission one is beaten solo; `test=crate` makes the world's authority put a heavy machine gun crate 2.75 tiles ahead of the start, just past where B appears. It presses **H**, then reads the room code from its console (`[PooketSlug/Net] Online … room ABCD-EU`). The code is only drawn on the canvas, so the log is how a script gets it.
2. B opens `?unlock=coop&test=crate&room=ABCD-EU` and presses **Enter**. In `quick` mode, both press **Q** instead.
3. It waits for `[PooketSlug/Net] … joined` on A, and both play for about 13 s.
4. **A must hand out the crate:** `Pickup … goes to player …` in A's log. Whoever touches it first claims it from A, so this covers the pickup table reaching B, a claim, and the grant back.
5. A presses **Esc** twice, and B must log `This client now runs the shared world`, which covers host migration.
6. It saves screenshots of both windows at each step and writes the merged, time-stamped console log to `console.log`.
7. It writes each check to `result.json`, with how many pickups were handed out, and exits non-zero if any check fails.

## Touch test

[`touch.mjs`](tools/touch.mjs) `<url> <outdir>` plays the game on an emulated phone: Playwright's *Pixel 7 landscape* profile, which has touch and a mobile browser's user agent.
- **Two thumbs need the DevTools protocol.** Playwright's `touchscreen` has one finger, so the script sends `Input.dispatchTouchEvent` with two touch points: one on the stick, one on FIRE.
- **It aims in HUD units.** The HUD is 360 units tall, so the script turns a pad position into page coordinates with the canvas's bounding box.
- **It checks by screenshots:** the touch hints on the title, the stick pushed and FIRE lit while the soldier runs and fires, and the pause screen after II.
- **It runs from the sandbox.** It needs no WebSockets, only the page.

## The sandbox's limits

- **The cloud sandbox can't run the co-op test.** Its egress proxy refuses WebSocket upgrades. The README in `/root/.ccr/` lists "WebSocket upgrades" under *Not supported through the proxy*. Photon in a browser only speaks secure WebSockets.
- **The attempt on 2026-10-14 got as far as the socket:**

  ```text
  WebSocket connection to 'wss://ns.photonengine.io/?libversion=5.1.20.0&…' failed:
    Error during WebSocket handshake: Unexpected response code: 502
  [Fusion] StartGame Failed: [StartGameResult: Ok:False, ShutdownReason: Error, ErrorMessage=DisconnectException: ExceptionOnConnect]
  [PooketSlug] Session ended: Error
  ```

  Everything before that ran in the deployed build. The online menu appeared, the network templates registered, and Fusion's runner started. Then the game returned to the title with an error, as designed.
- **Plain HTTPS to Photon works.** The proxy tunnels it, and Photon answers `403 HTTP not supported`. Only the upgrade is blocked.
- **So live co-op is tested on a normal network.** GitHub's runners allow WebSockets, so CI runs `coop.mjs` after each deploy ([the job](../build-pipeline/workflow.md#the-two-player-co-op-test-job)). The paths it doesn't drive need a person ([steps](../netcode/sessions.md#testing-by-hand)).

## Reading pages behind a bot challenge

- **Photon's documentation site is behind a JavaScript challenge,** so plain HTTP fetchers get the challenge page.
- [`fetch-docs.mjs`](tools/fetch-docs.mjs) `<outdir> <url>…` loads each page in Chromium, waits until the title stops saying "validating" or "just a moment", and saves the main text plus Photon, GitHub and Unity links.
- **Photon's SDK downloads need a login,** and the direct link answered 403. That's why the SDK reaches CI from a [private release](../netcode/sdk-in-ci.md).
