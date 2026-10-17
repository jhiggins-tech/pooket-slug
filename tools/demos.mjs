// Records the ⓘ kit screen's demo clips: one for each move, played by the game itself from a scripted scene with goons
// (?demo=all plays every move's demo in turn: KitGuide.cs has the scenes and the keys each one presses).
// The game records its own canvas for each demo, from its first frame to its last (PooketDemo.jslib); each demo runs on
// a fixed step, so a slow machine plays it in slow motion, and its clip is sped back up to its real speed. Each clip is
// the game's canvas, 640x360, H.264 with no sound: <outdir>/<id>.mp4 (e.g. tones2-fire.mp4), which the game plays from
// demos/ beside the page. Also writes demos.json (what was recorded), console.log and the raw recordings (raw/<id>.webm).
// Needs ffmpeg. CI runs it on each build, before it deploys (.github/workflows/webgl-pages.yml).
// usage: PW_PATH=... node demos.mjs <url> <outdir>
import { createRequire } from 'module';
import { execFileSync } from 'child_process';
import fs from 'fs';
import path from 'path';
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PW_PATH || 'playwright');

const [base, out] = process.argv.slice(2);
if (!base || !out) {
  console.error('usage: node demos.mjs <url> <outdir>');
  process.exit(2);
}
const url = base + (base.includes('?') ? '&' : '?') + 'demo=all';
const raw = path.join(out, 'raw');
fs.mkdirSync(raw, { recursive: true });
const ARGS = ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--autoplay-policy=no-user-gesture-required'];
const VIEW = { width: 1280, height: 720 };
const SIZE = { width: 640, height: 360 };
const TAG = String.raw`\[PooketSlug/Game\] `;
const LIST = new RegExp(TAG + String.raw`Demos: (\S+)`);
const END = new RegExp(TAG + String.raw`Demo end: (\S+) \((\d+) frames, ([\d.]+) s\)`);
const DONE = new RegExp(TAG + 'Demos done');
const LIMIT_MS = 6 * 60 * 1000; // all of them (they take under 2 minutes)
const STALL_MS = 90 * 1000; // with no demo starting or ending: a hung recording fails now, not at the limit
const PROGRESS = new RegExp(TAG + 'Demo (start|end): ');

const browser = await chromium.launch({ args: ARGS });
const page = await browser.newPage({ viewport: VIEW });
const T0 = Date.now();
const logs = [];
const ends = {}; // id → { frames, seconds } of game time, from the game's log
let ids = null, done = false, error = null, lastProgress = Date.now();
page.on('console', m => {
  const text = m.text();
  logs.push(`${((Date.now() - T0) / 1000).toFixed(2).padStart(7)} [${m.type()}] ${text}`);
  let hit;
  if (PROGRESS.test(text)) lastProgress = Date.now();
  if ((hit = LIST.exec(text))) ids = hit[1].split(',');
  else if ((hit = END.exec(text))) ends[hit[1]] = ends[hit[1]] || { frames: +hit[2], seconds: +hit[3] };
  else if (DONE.test(text)) done = true;
});
page.on('pageerror', e => { logs.push(`[pageerror] ${e.message}`); error = error || e.message; });

const clips = {}; // id → { webm: Buffer, seconds } (wall clock the recording took)
try {
  await page.goto(url, { waitUntil: 'load' });
  await page.waitForFunction(() => getComputedStyle(document.querySelector('#loader')).display === 'none', null, { timeout: 300000 });
  await page.waitForTimeout(1000);
  await page.locator('#unity-canvas').click();
  await page.keyboard.press('Enter'); // ?demo=all waits for it, so nothing plays under the loader
  const end = Date.now() + LIMIT_MS;
  lastProgress = Date.now();
  while (!done && !error && Date.now() < end && Date.now() - lastProgress < STALL_MS) await page.waitForTimeout(250);
  if (!done) throw new Error(error ? 'page error: ' + error
    : Date.now() - lastProgress >= STALL_MS ? `stuck: no demo started or ended for ${STALL_MS / 1000} s` : 'the demos never finished');
  await page.waitForFunction(n => Object.keys(window.pooketDemoClips || {}).length >= n, ids.length, { timeout: 30000 });
  for (const id of ids) {
    const clip = await page.evaluate(async id => {
      const c = window.pooketDemoClips[id];
      if (!c || !c.blob || !c.blob.size) return null; // nothing recorded (the recorder logs why)
      const bytes = new Uint8Array(await c.blob.arrayBuffer());
      let s = '';
      for (let i = 0; i < bytes.length; i += 0x8000) s += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));
      return { b64: btoa(s), seconds: c.seconds };
    }, id);
    if (clip) clips[id] = { webm: Buffer.from(clip.b64, 'base64'), seconds: clip.seconds };
  }
} catch (e) {
  error = e.message;
}
await browser.close();
fs.writeFileSync(path.join(out, 'console.log'), logs.join('\n') + '\n');
if (error) {
  console.error('Recording failed: ' + error);
  process.exit(1);
}

const summary = [];
for (const id of ids) {
  const clip = clips[id], game = ends[id];
  if (!clip || !game) {
    console.error(`${id}: not recorded`);
    continue;
  }
  const webm = path.join(raw, `${id}.webm`);
  fs.writeFileSync(webm, clip.webm);
  const speed = game.seconds / clip.seconds; // game seconds per recorded second: under 1 on a machine too slow to draw 30 frames a second
  const file = path.join(out, `${id}.mp4`);
  execFileSync('ffmpeg', ['-y', '-loglevel', 'error', '-i', webm,
    '-vf', `setpts=(PTS-STARTPTS)*${speed.toFixed(4)},fps=30,scale=${SIZE.width}:${SIZE.height}:flags=lanczos`,
    '-an', '-c:v', 'libx264', '-preset', 'slow', '-crf', '24', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', file]);
  const bytes = fs.statSync(file).size;
  summary.push({ id, gameSeconds: game.seconds, recordedSeconds: +clip.seconds.toFixed(2), speed: +speed.toFixed(3), bytes });
  console.log(`${id}: ${game.seconds} s of game in ${clip.seconds.toFixed(1)} s (×${(1 / speed).toFixed(2)} slow), ${(bytes / 1024).toFixed(0)} KB`);
}
fs.writeFileSync(path.join(out, 'demos.json'), JSON.stringify(summary, null, 2) + '\n');
if (summary.length < (ids || []).length || summary.length === 0) {
  console.error('Not every demo was recorded.');
  process.exit(1);
}
