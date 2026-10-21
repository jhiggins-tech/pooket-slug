// Two-player co-op check against a build, in two separate headless Chromium instances.
// There is one co-op room (CoopRoom.cs): CO-OP (H) in the den (START on the title opens it) joins it. Each round names a room of its own with
// ?room=, on every phone, so rounds running at once (and real players in the MATES room) never meet.
//   code mode (default): A joins (H), copies the invite link in the den (H again, as tapping the TV), which must carry
//                        the round's room, and B joins (H). Both have every tabk (?unlock=tank) and tones2's loaded:
//                        A's in the mission with it, so B's den must give it up and load kie's (one of each tabk).
//   boss mode:           as code mode, but both start at the boss (?test=boss) and fight it: B must see the boss
//                        tank's turret and shots from A's world. Nobody leaves.
//   dropin mode:         A starts alone and fights its way in, then B joins the mission under way. B
//                        must drop in partway with its own lives and a score of its own at 0, both play on, and
//                        (reported, not required: kills aren't certain) each sees the other's own score. Nobody leaves.
//   handover mode:       rooms: as code mode, then A's page stalls for 32 s (its main thread held busy, as a phone
//                        freezes a game sent to the background). B must notice A's beat stop, ask Photon for the
//                        shared world and run it, well before Photon would time A out, and play on. Then A comes
//                        back (reported, not required: it may have been timed out by then) and should follow B's world.
//   watch mode:          A and B join (H) and play, then C joins (H) the room with both seats taken and watches it
//                        (?unlock=coop counts the mission as cleared): C must follow the game with no soldier of its
//                        own, and A's room must count C watching. Nobody leaves.
//   debrief mode:        as scene mode, with ?test=debrief: the room's first round is won at once, both phones get
//                        their debrief (XP and rank), tap through it and land back in the room's den together, then
//                        sit down and start the next round, which must not be won already. Nobody leaves.
//   scene mode:          as code mode, but both sit down in the den together and start at once, with ?test=scene, so
//                        a stand-in scene plays on both phones (Mission one is single player only, so its own scenes
//                        never play in co-op): both tap through it, and it must move on for both, together, each shot
//                        on their taps (docs/design/cutscenes/README.md, How every cutscene plays). Nobody leaves.
// Then (code mode) both play for a while, A leaves (Esc Esc), and B should take over the shared world.
// Both open the game with ?unlock=coop, since a new browser hides co-op until Mission one is beaten solo, and with
// ?test=crate, which puts a crate just ahead of the start: one of them must be handed it by A's world.
// Writes screenshots, console.log and result.json to <outdir>, and exits non-zero if any check fails.
// CI runs it against each build (.github/workflows/webgl-pages.yml).
// usage: PW_PATH=... node coop.mjs <url> <outdir> [code|boss|dropin|handover|watch|scene|debrief]
import { createRequire } from 'module';
import fs from 'fs';
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PW_PATH || 'playwright');

const [base, out, mode = 'code'] = process.argv.slice(2);
const BOSS = mode === 'boss', SCENE = mode === 'scene', DEBRIEF = mode === 'debrief', DROPIN = mode === 'dropin', HANDOVER = mode === 'handover', WATCH = mode === 'watch';
const ROOM = 'CI' + Math.random().toString(36).slice(2, 8).toUpperCase(); // this round's room
const url = base + (base.includes('?') ? '&' : '?') + 'room=' + ROOM + '&unlock=coop' + (mode === 'code' ? ',tank' : '') + '&test=' + (BOSS ? 'boss' : SCENE ? 'crate,scene' : DEBRIEF ? 'debrief' : 'crate');
const TOGETHER = SCENE || DEBRIEF; // both sit down in the den and start together
fs.mkdirSync(out, { recursive: true });
const T0 = Date.now();
const t = () => ((Date.now() - T0) / 1000).toFixed(1).padStart(6);
const TAG = String.raw`\[PooketSlug(?:/\w+)?\]`; // "[PooketSlug/Net] …" (older builds: "[PooketSlug] …")
const ARGS = ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--autoplay-policy=no-user-gesture-required'];

// Each check is false until it passes; result.json records them all.
const checks = WATCH
  ? { hostOnline: false, guestOnline: false, joined: false, played: false, watcherIn: false, watching: false, counted: false }
  : HANDOVER
  ? { hostOnline: false, guestOnline: false, joined: false, played: false, asked: false, tookOver: false, quick: false, playedOn: false }
  : DROPIN
  ? { hostOnline: false, guestOnline: false, joined: false, droppedIn: false, played: false }
  : BOSS
  ? { hostOnline: false, guestOnline: false, joined: false, played: false, bossIntro: false, bossFought: false, bossFollowed: false, bossFired: false }
  : DEBRIEF
  ? { hostOnline: false, guestOnline: false, joined: false, bothDebriefs: false, bothInDen: false, nextRound: false, played: false }
  : SCENE
    ? { hostOnline: false, guestOnline: false, joined: false, bothInScene: false, waited: false, tappedThrough: false, together: false, played: false }
    : { hostOnline: false, guestOnline: false, joined: false, invite: false, tabk: false, played: false, pickup: false, takeover: false };
const GRANT = new RegExp(TAG + String.raw` Pickup \d+ .* goes to player`); // the master handing out a pickup
let failure = null;

async function player(name) {
  const browser = await chromium.launch({ args: ARGS });
  const context = await browser.newContext({ viewport: { width: 960, height: 720 }, permissions: ['clipboard-read', 'clipboard-write'] });
  const page = await context.newPage();
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

/** Press `key` until the game logs `re`: a key pressed just after loading can land before the title takes keys. */
async function pressUntil(p, key, re, tries = 5) {
  for (let i = 0; i < tries; i++) {
    await p.page.keyboard.press(key);
    const hit = await waitLog(p, re, 3000);
    if (hit) return hit;
  }
  return null;
}

/** START on the title opens the den, then CO-OP (H) there, into this round's room: true once the game says it's online there. */
async function goOnline(p) {
  if (!(await pressUntil(p, 'Enter', new RegExp(TAG + ' Den open: Solo')))) throw new Error(`${p.name} never got from the title to the den`);
  if (!(await pressUntil(p, 'h', new RegExp(TAG + ` Joining room ${ROOM}`)))) throw new Error(`${p.name} never pressed CO-OP`);
  return !!(await waitLog(p, new RegExp(TAG + ` Online .*room ${ROOM}`), 90000));
}

const A = await player('A');
const B = await player('B');
const C = WATCH ? await player('C') : null;
try {
  if (WATCH) {
    await load(A, url);
    checks.hostOnline = await goOnline(A);
    if (!checks.hostOnline) throw new Error('A never got online');
    console.log(`${t()} A is in ${ROOM}`);
    await denStart(A);
    await A.page.waitForTimeout(2000);
    await load(B, url);
    checks.guestOnline = await goOnline(B);
    if (!checks.guestOnline) throw new Error('B never got online');
    await denStart(B);
    checks.joined = !!(await waitLog(A, new RegExp(TAG + ' .* joined'), 30000));
    await Promise.all([play(A, 'ArrowRight', 5000), play(B, 'ArrowRight', 5000)]);

    await load(C, url);
    const before = A.logs.length; // A's count from here on: C's arrival, not B's
    checks.watcherIn = await goOnline(C) && !!(await waitLog(C, new RegExp(TAG + ` Online .*room ${ROOM}, watching`), 5000));
    await shot(C, '1-joined');
    if (!checks.watcherIn) throw new Error(`C never watched ${ROOM}`);
    checks.watching = !!(await waitLog(C, new RegExp(TAG + ` Watching room ${ROOM}`), 20000));
    await Promise.all([play(A, 'ArrowRight', 6000), play(B, 'ArrowRight', 6000)]);
    await Promise.all([shot(A, '2-watched'), shot(B, '2-watched'), shot(C, '2-watching')]);
    const counts = A.logs.slice(before).filter(l => new RegExp(TAG + ` \\d+ watching room ${ROOM}`).test(l));
    checks.counted = counts.length > 0 && / 1 watching room /.test(counts[counts.length - 1]);
    console.log(`${t()} A's room counts: ${counts.map(l => l.match(/(\d+) watching/)[1]).join(', ') || 'none'}`);
    checks.played = ![...A.logs, ...B.logs, ...C.logs].some(l => /\[pageerror\]|Disconnected from Photon|Session ended/.test(l));
    throw null; // done: on to the results
  }
  await load(A, url);
  await shot(A, '0-title');

  checks.hostOnline = await goOnline(A);
  console.log(`${t()} A is in ${ROOM}`);
  if (!checks.hostOnline) throw new Error('A never got online');
  if (mode === 'code') {
    // rooms: the TV's invite (H in a room's den, as a tap on the TV) copies the room's link on a computer
    await denOpen(A);
    await A.page.keyboard.press('h');
    const copied = await waitLog(A, new RegExp(TAG + ' Invite (copied|shared)'), 10000);
    const clip = copied ? await A.page.evaluate(() => navigator.clipboard.readText()).catch(e => 'unreadable: ' + e.message) : '';
    checks.invite = !!copied && clip.endsWith('?room=' + ROOM);
    console.log(`${t()} invite: ${copied ? 'copied ' + clip : 'NO'}`);
    await shot(A, 'den-invited');
  }
  if (TOGETHER) await denOpen(A); // A waits in the den for B, so they start together
  else {
    await denStart(A);
    await A.page.waitForTimeout(3000);
    if (DROPIN) await play(A, 'ArrowRight', 20000); // well into the mission before B turns up
  }
  await shot(A, '1-online');

  await load(B, url);
  await shot(B, '0-title');
  if (!(await goOnline(B))) throw new Error('B never got online');
  checks.guestOnline = true;
  if (TOGETHER) {
    await denOpen(B);
    await B.page.waitForTimeout(1500);
    await Promise.all([A.page.keyboard.press('Enter'), B.page.keyboard.press('Enter')]); // both sit down and press START
  } else await denStart(B);
  if (!(await waitLog(A, new RegExp(TAG + ' .* joined'), 30000))) throw new Error('A never saw B join');
  checks.joined = true;
  if (mode === 'code') {
    // one of each tabk: A took tones2's into the mission, so B's den loads the next one in bay order
    const gave = await waitLog(B, /Tabk given way: the partner has TONES2'S TABK in the mission; loaded KIE'S TABK/, 15000);
    checks.tabk = !!gave && !A.logs.some(l => /Tabk given way/.test(l));
    console.log(`${t()} tabks: ${gave ? 'B gave way' : 'B never gave way'}`);
  }
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
  } else if (HANDOVER) {
    await Promise.all([play(A, 'ArrowRight', 5000), play(B, 'ArrowRight', 5000)]);
    checks.played = ![...A.logs, ...B.logs].some(l => /\[pageerror\]|Disconnected from Photon|Session ended/.test(l));
    // A's phone sends the game to the background: its page stops running, but its connection stays open. Headless
    // Chromium won't freeze a page (Page.setWebLifecycleState didn't take in CI), so A's main thread is held busy
    // instead: the game, and everything it sends Photon, stops just the same.
    const STALL = 32000;
    const frozeAt = Date.now();
    const stalled = A.page.evaluate(ms => { const end = performance.now() + ms; while (performance.now() < end) {} }, STALL).catch(() => {});
    await A.page.waitForTimeout(300);
    const answers = () => Promise.race([A.page.evaluate(() => 1).then(() => true), new Promise(r => setTimeout(() => r(false), 1500))]);
    const frozen = !(await answers());
    console.log(`${t()} A frozen: ${frozen ? 'yes' : 'NO, it still answers'}`);
    const playing = play(B, 'ArrowRight', 12000);
    const asked = await waitLog(B, /quiet for .* asking to run the shared world/, 15000);
    checks.asked = !!asked;
    const took = await waitLog(B, /now runs the shared world/, 20000);
    checks.tookOver = !!took;
    const secs = took ? (Date.now() - frozeAt) / 1000 : null;
    checks.quick = secs !== null && secs < 10; // Photon would only time A out after about 10 s
    console.log(`${t()} handover: ${took ? `B runs the shared world, ${secs.toFixed(1)} s after A froze` : 'NO'}`);
    await playing;
    await shot(B, '3-alone');
    checks.playedOn = !B.logs.some(l => /\[pageerror\]|Disconnected from Photon|Session ended/.test(l));
    await stalled;
    console.log(`${t()} A back`);
    const back = await waitLog(A, /Another client runs the shared world/, 15000);
    console.log(`${t()} A back: ${back ? 'follows B\'s world' : A.logs.some(l => /Session ended|Disconnected/.test(l)) ? 'timed out by Photon' : 'no word'}`);
    await Promise.all([play(A, 'ArrowRight', 4000), play(B, 'ArrowRight', 4000)]);
    await Promise.all([shot(A, '4-back'), shot(B, '4-back')]);
  } else if (DROPIN) {
    const line = await waitLog(B, /Dropped in to a mission under way/, 15000);
    checks.droppedIn = !!line;
    console.log(`${t()} drop-in: ${line ? line.replace(/^.*Dropped in/, 'dropped in') : 'NO'}`);
    await Promise.all([play(A, 'ArrowRight', 8000), play(B, 'ArrowRight', 8000)]);
    await Promise.all([shot(A, '3-dropped-in'), shot(B, '3-dropped-in')]);
    await Promise.all([play(A, null, 6000), play(B, null, 6000)]);
    await Promise.all([shot(A, '4-own-scores'), shot(B, '4-own-scores')]);
    checks.played = ![...A.logs, ...B.logs].some(l => /\[pageerror\]|Disconnected from Photon|Session ended/.test(l));
    const scored = p => p.logs.some(l => /First points of our own/.test(l)), seen = p => p.logs.some(l => /own score: \d+/.test(l));
    console.log(`${t()} own scores: A scored ${scored(A)}, B saw it ${seen(B)}; B scored ${scored(B)}, A saw it ${seen(A)}`);
  } else if (DEBRIEF) {
    const debriefs = [await waitLog(A, /Debrief: \d+ XP/, 30000), await waitLog(B, /Debrief: \d+ XP/, 30000)];
    checks.bothDebriefs = debriefs.every(Boolean);
    console.log(`${t()} debriefs: A ${debriefs[0] ? 'yes' : 'NO'}, B ${debriefs[1] ? 'yes' : 'NO'}`);
    await Promise.all([shot(A, '3-debrief'), shot(B, '3-debrief')]);
    if (!checks.bothDebriefs) throw new Error('the win never gave both phones a debrief');
    // tap through it (fire), B a few seconds behind A, so A waits in the den for B
    const through = async (p, ms) => {
      const end = Date.now() + ms;
      while (Date.now() < end && !p.logs.some(l => /back to the co-op den/.test(l))) {
        await p.page.keyboard.down('x'); await p.page.waitForTimeout(250); await p.page.keyboard.up('x');
        await p.page.waitForTimeout(350);
      }
    };
    await through(A, 60000);
    await B.page.waitForTimeout(4000);
    await through(B, 60000);
    const dens = p => p.logs.filter(l => /Den open: Coop/.test(l)).length;
    checks.bothInDen = dens(A) >= 2 && dens(B) >= 2;
    console.log(`${t()} back in the den: A ${dens(A) >= 2 ? 'yes' : 'NO'}, B ${dens(B) >= 2 ? 'yes' : 'NO'}`);
    await Promise.all([shot(A, '4-den-again'), shot(B, '4-den-again')]);
    if (!checks.bothInDen) throw new Error('the debrief never led both phones back to the den');
    await A.page.waitForTimeout(1500);
    await Promise.all([A.page.keyboard.press('Enter'), B.page.keyboard.press('Enter')]); // both sit down and press START
    const next = [await waitLog(A, /Co-op mission start: .*, round 1/, 30000), await waitLog(B, /Co-op mission start: .*, round 1/, 30000)];
    await Promise.all([play(A, 'ArrowRight', 5000), play(B, 'ArrowRight', 5000)]);
    const wonAgain = p => p.logs.filter(l => /Debrief: \d+ XP/.test(l)).length > 1;
    checks.nextRound = next.every(Boolean) && !wonAgain(A) && !wonAgain(B);
    console.log(`${t()} next round: A ${next[0] ? 'yes' : 'NO'}, B ${next[1] ? 'yes' : 'NO'}, won again: ${wonAgain(A) || wonAgain(B)}`);
    await Promise.all([shot(A, '5-next-round'), shot(B, '5-next-round')]);
    checks.played = ![...A.logs, ...B.logs].some(l => /\[pageerror\]|Disconnected from Photon|Session ended/.test(l));
  } else if (BOSS) {
    // Both walk up past the gate, far enough for the camera to show the whole tank (it wakes then), and fight
    // from there. B's tank follows A's (its network object) and fires when A's does. A tabk parked by the map's end
    // (kcaj's, at the cold store) wakes only with the camera there, so they walk on until it does.
    await Promise.all([walk(A, 'ArrowRight', 3000), walk(B, 'ArrowRight', 3000)]);
    for (let i = 0; i < 6 && !A.logs.some(l => /Boss intro/.test(l)); i++)
      await Promise.all([walk(A, 'ArrowRight', 1000), walk(B, 'ArrowRight', 1000)]);
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
  if (e !== null) {
  failure = e.message;
  console.log(`${t()} FAILED: ${e.message}`);
  await Promise.all([shot(A, 'x-fail').catch(() => {}), shot(B, 'x-fail').catch(() => {})]);
  }
} finally {
  const keep = l => !/\[debug\]/.test(l);
  const all = [...A.logs, ...B.logs, ...(C ? C.logs : [])].filter(keep).sort((a, b) => parseFloat(a) - parseFloat(b));
  fs.writeFileSync(`${out}/console.log`, all.join('\n') + '\n');
  console.log(all.filter(l => /PooketSlug|error|exception|warn|Fusion|Photon/i.test(l)).slice(-120).join('\n'));
  const ok = !failure && Object.values(checks).every(Boolean);
  const pickups = all.filter(l => GRANT.test(l)).length;
  fs.writeFileSync(`${out}/result.json`, JSON.stringify({ mode, ok, failure, checks, pickups, seconds: Math.round((Date.now() - T0) / 1000) }, null, 2) + '\n');
  console.log(`${t()} result: ${ok ? 'PASS' : 'FAIL'} ${JSON.stringify(checks)}, ${pickups} pickup(s) given out`);
  await A.browser.close();
  await B.browser.close();
  if (C) await C.browser.close();
  if (!ok) process.exitCode = 1;
}
