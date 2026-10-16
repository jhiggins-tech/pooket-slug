// Two-player co-op check against a build, in two separate headless Chromium instances.
//   code mode (default): A hosts (H), B opens the invite link ?room=CODE and presses Enter.
//   quick mode:          both press Q (quick match).
//   boss mode:           as code mode, but both start at the boss (?test=boss) and fight it: B must see the boss
//                        tank's turret and shots from A's world. Nobody leaves.
//   scene mode:          as code mode, but both sit down in the den together and start at once, with ?test=scene, so
//                        a stand-in scene plays on both phones (Mission one is single player only, so its own scenes
//                        never play in co-op): both tap through it, and it must move on for both, together, each shot
//                        on their taps (docs/design/story.md, Cutscenes). Nobody leaves.
// Then (code and quick) both play for a while, A leaves (Esc Esc), and B should take over the shared world.
// Both open the game with ?unlock=coop, since a new browser hides co-op until Mission one is beaten solo, and with
// ?test=crate, which puts a crate just ahead of the start: one of them must be handed it by A's world.
// Writes screenshots, console.log and result.json to <outdir>, and exits non-zero if any check fails.
// CI runs it against each build (.github/workflows/webgl-pages.yml).
// usage: PW_PATH=... node coop.mjs <url> <outdir> [code|quick]
import { createRequire } from 'module';
import fs from 'fs';
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PW_PATH || 'playwright');

const [base, out, mode = 'code'] = process.argv.slice(2);
const BOSS = mode === 'boss', SCENE = mode === 'scene';
const url = base + (base.includes('?') ? '&' : '?') + 'unlock=coop&test=' + (BOSS ? 'boss' : SCENE ? 'crate,scene' : 'crate');
fs.mkdirSync(out, { recursive: true });
const T0 = Date.now();
const t = () => ((Date.now() - T0) / 1000).toFixed(1).padStart(6);
const TAG = String.raw`\[PooketSlug(?:/\w+)?\]`; // "[PooketSlug/Net] …" (older builds: "[PooketSlug] …")
const ARGS = ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--autoplay-policy=no-user-gesture-required'];

// Each check is false until it passes; result.json records them all.
const checks = BOSS
  ? { hostOnline: false, guestOnline: false, joined: false, played: false, bossIntro: false, bossFought: false, bossFollowed: false, bossFired: false }
  : SCENE
    ? { hostOnline: false, guestOnline: false, joined: false, bothInScene: false, waited: false, tappedThrough: false, together: false, played: false }
    : { hostOnline: false, guestOnline: false, joined: false, played: false, pickup: false, takeover: false };
const GRANT = new RegExp(TAG + String.raw` Pickup \d+ .* goes to player`); // the master handing out a pickup
let failure = null;

async function player(name) {
  const browser = await chromium.launch({ args: ARGS });
  const page = await browser.newPage({ viewport: { width: 960, height: 720 } });
  const logs = [];
  page.on('console', m => logs.push(`${t()} ${name} [${m.type()}] ${m.text()}`));
  page.on('pageerror', e => logs.push(`${t()} ${name} [pageerror] ${e.message}`));
  return { name, browser, page, logs };
}

async function load(p, u) {
  await p.page.goto(u, { waitUntil: 'load' });
  await p.page.waitForFunction(() => getComputedStyle(document.querySelector('#loader')).display === 'none', null, { timeout: 300000 });
  console.log(`${t()} ${p.name} loaded ${u}`);
  await p.page.waitForTimeout(2500);
  await p.page.locator('#unity-canvas').click();
}

const shot = (p, tag) => p.page.screenshot({ path: `${out}/${p.name}-${tag}.png` });

async function waitLog(p, re, ms) {
  const end = Date.now() + ms;
  while (Date.now() < end) {
    const hit = p.logs.find(l => re.test(l));
    if (hit) return hit;
    await p.page.waitForTimeout(250);
  }
  return null;
}

/** Hold a direction for `ms` without jumping, so nobody hops over the test crate. */
async function walk(p, dir, ms) {
  await p.page.keyboard.down(dir);
  await p.page.waitForTimeout(ms);
  await p.page.keyboard.up(dir);
}

/** Hold a direction (or none: stand and fight) for `ms` while tapping fire (and jumping, and rocket jumping, now and then). */
async function play(p, dir, ms) {
  const kb = p.page.keyboard;
  if (dir) await kb.down(dir);
  const end = Date.now() + ms;
  for (let i = 0; Date.now() < end; i++) {
    await kb.down('x'); await p.page.waitForTimeout(80); await kb.up('x');
    if (i % 5 === 2) { await kb.down('z'); await p.page.waitForTimeout(180); await kb.up('z'); }
    if (i % 9 === 4) { await kb.down('c'); await p.page.waitForTimeout(80); await kb.up('c'); } // tones2's rocket jump: its mud is sent to the partner
    await p.page.waitForTimeout(120);
  }
  if (dir) await kb.up(dir);
}

/** Once online, the den (the lobby) opens: Enter sits whoever's free on the couch and presses START. */
async function denStart(p) {
  await denOpen(p);
  await p.page.keyboard.press('Enter');
  if (!(await waitLog(p, new RegExp(TAG + ' Den START'), 15000))) throw new Error(`${p.name} pressed START in the den but it never started`);
}

async function denOpen(p) {
  if (!(await waitLog(p, new RegExp(TAG + ' Den open: Coop'), 90000))) throw new Error(`${p.name} never reached the den`);
  await p.page.waitForTimeout(500);
  await shot(p, 'den');
}

/** Seconds into the run that a log line was written (each line starts with it). */
const at = line => parseFloat(line);

/** Taps (fire) every so often until the scene's over on this phone, or `ms` passes. */
async function tapThrough(p, ms) {
  const end = Date.now() + ms;
  while (Date.now() < end && !p.logs.some(l => /Cutscene over: scene-coop-test/.test(l))) {
    // held a while: a slow runner draws a few frames a second, and a quicker press can fall between two
    await p.page.keyboard.down('x'); await p.page.waitForTimeout(250); await p.page.keyboard.up('x');
    await p.page.waitForTimeout(350);
  }
}

const A = await player('A');
const B = await player('B');
try {
  await load(A, url);
  await shot(A, '0-title');

  let joinUrl = url;
  if (mode === 'quick') {
    await A.page.keyboard.press('q');
    checks.hostOnline = !!(await waitLog(A, new RegExp(TAG + ' Online '), 90000));
  } else {
    await A.page.keyboard.press('h');
    const line = await waitLog(A, new RegExp(TAG + ' Online .*room ([A-Z0-9]+(?:-[A-Z]+)?)'), 90000); // ABCD (older builds: ABCD-EU)
    if (!line) throw new Error('A never got a room code');
    checks.hostOnline = true;
    const code = line.match(/room ([A-Z0-9]+(?:-[A-Z]+)?)/)[1];
    console.log(`${t()} A hosts ${code}`);
    joinUrl = url + (url.includes('?') ? '&' : '?') + 'room=' + code;
  }
  if (!checks.hostOnline) throw new Error('A never got online');
  if (SCENE) await denOpen(A); // A waits in the den for B, so they start together
  else {
    await denStart(A);
    await A.page.waitForTimeout(3000);
  }
  await shot(A, '1-online');

  await load(B, joinUrl);
  await shot(B, '0-title');
  await B.page.keyboard.press(mode === 'quick' ? 'q' : 'Enter');
  if (!(await waitLog(B, new RegExp(TAG + ' Online '), 90000))) throw new Error('B never got online');
  checks.guestOnline = true;
  if (SCENE) {
    await denOpen(B);
    await B.page.waitForTimeout(1500);
    await Promise.all([A.page.keyboard.press('Enter'), B.page.keyboard.press('Enter')]); // both sit down and press START
  } else await denStart(B);
  if (!(await waitLog(A, new RegExp(TAG + ' .* joined'), 30000))) throw new Error('A never saw B join');
  checks.joined = true;
  await B.page.waitForTimeout(4000);
  await Promise.all([shot(A, '2-together'), shot(B, '2-together')]);

  if (SCENE) {
    const start = [await waitLog(A, /Cutscene: scene-coop-test, with a partner/, 30000), await waitLog(B, /Cutscene: scene-coop-test, with a partner/, 30000)];
    checks.bothInScene = start.every(Boolean);
    if (!checks.bothInScene) throw new Error('the scene never played on both phones');
    await A.page.waitForTimeout(1000);
    // A taps alone first: B's phone must show it, and nobody moves on yet
    await A.page.keyboard.down('x'); await A.page.waitForTimeout(80); await A.page.keyboard.up('x');
    await A.page.waitForTimeout(1500);
    await Promise.all([shot(A, '3-a-tapped'), shot(B, '3-a-tapped')]);
    checks.waited = !A.logs.some(l => /Cutscene shot 2/.test(l)) && !B.logs.some(l => /Cutscene shot 2/.test(l));
    await Promise.all([tapThrough(A, 30000), tapThrough(B, 30000)]);
    const over = [await waitLog(A, /Cutscene over: scene-coop-test/, 5000), await waitLog(B, /Cutscene over: scene-coop-test/, 5000)];
    const began = Math.max(...start.map(at));
    // CI's runner plays slower than real time, so the clock can't say whether taps did it: the game logs each
    // shot that moved on with time left. Of the five moves, four or more should be tapped on (a tap can land in the intro).
    const tapped = p => p.logs.filter(l => /Cutscene shot \d+ of \d+, tapped on/.test(l)).length;
    checks.tappedThrough = over.every(Boolean) && tapped(A) >= 4 && tapped(B) >= 4;
    checks.together = over.every(Boolean) && Math.abs(at(over[0]) - at(over[1])) < 4;
    console.log(`${t()} the scene took ${over.map(l => l ? (at(l) - began).toFixed(1) + ' s' : 'forever').join(' and ')}`);
    await Promise.all([play(A, 'ArrowRight', 4000), play(B, 'ArrowRight', 4000)]);
    await Promise.all([shot(A, '4-playing'), shot(B, '4-playing')]);
    checks.played = ![...A.logs, ...B.logs].some(l => /\[pageerror\]|Disconnected from Photon|Session ended/.test(l));
  } else if (BOSS) {
    // Both walk up past the gate, far enough for the camera to show the whole tank (it wakes then), and fight
    // from there. B's tank follows A's (its network object) and fires when A's does.
    await Promise.all([walk(A, 'ArrowRight', 3000), walk(B, 'ArrowRight', 3000)]);
    for (let i = 0; i < 4; i++) {
      await Promise.all([play(A, null, 5000), play(B, null, 5000)]);
      await Promise.all([shot(A, `3-boss-${i}`), shot(B, `3-boss-${i}`)]);
    }
    checks.played = ![...A.logs, ...B.logs].some(l => /\[pageerror\]|Disconnected from Photon|Session ended/.test(l));
    checks.bossIntro = !!(await waitLog(A, /Boss intro/, 1000)) && !!(await waitLog(B, /Boss intro/, 1000));
    checks.bossFought = !!(await waitLog(A, /Boss fight on/, 1000));
    checks.bossFollowed = !!(await waitLog(B, /Following the boss tank's turret/, 5000));
    checks.bossFired = !!(await waitLog(B, /boss tank fired on the authority's screen/, 5000));
  } else {
    // Both walk onto the test crate first: play() jumps about half a second in, which is about when a soldier
    // reaches the crate, so on a slow frame both could hop over it and the pickup check would fail.
    await Promise.all([walk(A, 'ArrowRight', 1200), walk(B, 'ArrowRight', 1200)]);
    // both advance and shoot
    await Promise.all([play(A, 'ArrowRight', 7000), play(B, 'ArrowRight', 7000)]);
    await Promise.all([shot(A, '3-fighting'), shot(B, '3-fighting')]);
    await Promise.all([play(A, 'ArrowRight', 6000), play(B, 'ArrowRight', 6000)]);
    await Promise.all([shot(A, '4-further'), shot(B, '4-further')]);
    checks.played = ![...A.logs, ...B.logs].some(l => /\[pageerror\]|Disconnected from Photon|Session ended/.test(l));
    checks.pickup = !!(await waitLog(A, GRANT, 5000));

    // the master client leaves: B should inherit the shared world and carry on alone
    await A.page.keyboard.press('Escape');
    await A.page.waitForTimeout(300);
    await A.page.keyboard.press('Escape');
    const takeover = await waitLog(B, /now runs the shared world/, 30000);
    checks.takeover = !!takeover;
    console.log(`${t()} takeover: ${takeover ? 'yes' : 'NO'}`);
    await play(B, 'ArrowRight', 4000);
    await Promise.all([shot(A, '5-left'), shot(B, '5-alone')]);
  }
} catch (e) {
  failure = e.message;
  console.log(`${t()} FAILED: ${e.message}`);
  await Promise.all([shot(A, 'x-fail').catch(() => {}), shot(B, 'x-fail').catch(() => {})]);
} finally {
  const keep = l => !/\[debug\]/.test(l);
  const all = [...A.logs, ...B.logs].filter(keep).sort((a, b) => parseFloat(a) - parseFloat(b));
  fs.writeFileSync(`${out}/console.log`, all.join('\n') + '\n');
  console.log(all.filter(l => /PooketSlug|error|exception|warn|Fusion|Photon/i.test(l)).slice(-120).join('\n'));
  const ok = !failure && Object.values(checks).every(Boolean);
  const pickups = all.filter(l => GRANT.test(l)).length;
  fs.writeFileSync(`${out}/result.json`, JSON.stringify({ mode, ok, failure, checks, pickups, seconds: Math.round((Date.now() - T0) / 1000) }, null, 2) + '\n');
  console.log(`${t()} result: ${ok ? 'PASS' : 'FAIL'} ${JSON.stringify(checks)}, ${pickups} pickup(s) given out`);
  await A.browser.close();
  await B.browser.close();
  if (!ok) process.exitCode = 1;
}
