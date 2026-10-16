// Records the ⓘ kit screen's demo clips: one for each move, played by the game itself from a scripted scene with goons
// (?demo=all plays every move's demo in turn: KitGuide.cs has the scenes and the keys each one presses).
// Chromium records the page while the game plays them; each demo runs on a fixed step, so a slow machine plays it in
// slow motion, and its clip is cut out of the recording at the times the game logged and sped back up to its real
// speed. Each clip is the game's canvas, 640x360, H.264 with no sound: <outdir>/<id>.mp4 (e.g. tones2-fire.mp4), which
// the game plays from demos/ beside the page. Also writes demos.json (what was recorded), console.log and the raw video.
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
const START = new RegExp(TAG + String.raw`Demo start: (\S+)`);
const END = new RegExp(TAG + String.raw`Demo end: (\S+) \((\d+) frames, ([\d.]+) s\)`);
const DONE = new RegExp(TAG + 'Demos done');
const LIMIT_MS = 15 * 60 * 1000;

const browser = await chromium.launch({ args: ARGS });
const context = await browser.newContext({ viewport: VIEW, recordVideo: { dir: raw, size: VIEW } });
const page = await context.newPage();
const T0 = Date.now(); // the recording starts with the page
const logs = [];
const demos = []; // { id, start, end, frames, seconds }, in the order played
let done = false, error = null;
page.on('console', m => {
  const now = Date.now(), text = m.text();
  logs.push(`${((now - T0) / 1000).toFixed(2).padStart(7)} [${m.type()}] ${text}`);
  let hit;
  if ((hit = START.exec(text))) demos.push({ id: hit[1], start: now });
  else if ((hit = END.exec(text))) {
    const d = demos[demos.length - 1];
    if (d && d.id === hit[1] && d.end === undefined) Object.assign(d, { end: now, frames: +hit[2], seconds: +hit[3] });
  } else if (DONE.test(text)) done = true;
});
page.on('pageerror', e => { logs.push(`[pageerror] ${e.message}`); error = error || e.message; });

try {
  await page.goto(url, { waitUntil: 'load' });
  await page.waitForFunction(() => getComputedStyle(document.querySelector('#loader')).display === 'none', null, { timeout: 300000 });
  await page.waitForTimeout(1000);
  await page.locator('#unity-canvas').click();
  await page.keyboard.press('Enter'); // ?demo=all waits for it, so nothing plays under the loader
  const end = Date.now() + LIMIT_MS;
  while (!done && !error && Date.now() < end) await page.waitForTimeout(250);
  if (!done) throw new Error(error ? 'page error: ' + error : 'the demos never finished');
} catch (e) {
  error = e.message;
}
const box = await page.locator('#unity-canvas').boundingBox().catch(() => null);
const video = page.video();
await context.close(); // finishes the recording
await browser.close();
fs.writeFileSync(path.join(out, 'console.log'), logs.join('\n') + '\n');
if (error) {
  console.error('Recording failed: ' + error);
  process.exit(1);
}
const webm = await video.path();
const even = n => Math.max(2, Math.floor(n / 2) * 2);
const crop = `crop=${even(box.width)}:${even(box.height)}:${Math.round(box.x)}:${Math.round(box.y)}`;

const seen = new Set(), summary = [];
for (const d of demos) {
  if (d.end === undefined || seen.has(d.id)) continue; // ?demo=all loops: keep each move's first full run
  seen.add(d.id);
  const real = (d.end - d.start) / 1000;
  // a little off each end: the console's timestamps lag the recording, so a cut right at the start shows the last demo
  const from = (d.start - T0) / 1000 + 0.3, length = Math.max(0.5, real - 0.45);
  const speed = d.seconds / real; // game seconds per recorded second: under 1 on a machine too slow to draw 30 frames a second
  const file = path.join(out, `${d.id}.mp4`);
  execFileSync('ffmpeg', ['-y', '-loglevel', 'error', '-ss', from.toFixed(3), '-t', length.toFixed(3), '-i', webm,
    '-vf', `${crop},setpts=(PTS-STARTPTS)*${speed.toFixed(4)},fps=30,scale=${SIZE.width}:${SIZE.height}:flags=lanczos`,
    '-an', '-c:v', 'libx264', '-preset', 'slow', '-crf', '24', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', file]);
  const bytes = fs.statSync(file).size;
  summary.push({ id: d.id, gameSeconds: d.seconds, recordedSeconds: +real.toFixed(2), speed: +speed.toFixed(3), bytes });
  console.log(`${d.id}: ${d.seconds} s of game in ${real.toFixed(1)} s (×${(1 / speed).toFixed(2)} slow), ${(bytes / 1024).toFixed(0)} KB`);
}
fs.writeFileSync(path.join(out, 'demos.json'), JSON.stringify(summary, null, 2) + '\n');
if (summary.length === 0) {
  console.error('No demo was recorded.');
  process.exit(1);
}
