// Plays a deployed build on an emulated phone held sideways, with two thumbs: one on the touch pad's stick, one
// on FIRE. Playwright's own touch API has one finger, so the touches go through Chromium's DevTools protocol.
// Saves screenshots and the console log to <outdir>; the log has the projectile pool's use with ?log=game:debug.
// usage: PW_PATH=$(npm root -g)/playwright node touch.mjs <url> <outdir>
import { createRequire } from 'module';
import fs from 'fs';
const require = createRequire(import.meta.url);
const { chromium, devices } = require(process.env.PW_PATH || 'playwright');

const [base, out] = process.argv.slice(2);
const url = base + (base.includes('?') ? '&' : '?') + 'log=game:debug';
fs.mkdirSync(out, { recursive: true });
const browser = await chromium.launch({
  args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--autoplay-policy=no-user-gesture-required'],
});
const context = await browser.newContext({ ...devices['Pixel 7 landscape'] });
const page = await context.newPage();
const cdp = await context.newCDPSession(page);
const logs = [];
const T0 = Date.now();
const t = () => ((Date.now() - T0) / 1000).toFixed(1);
page.on('console', m => logs.push(`${t()} [${m.type()}] ${m.text()}`));
page.on('pageerror', e => logs.push(`${t()} [pageerror] ${e.message}`));

await page.goto(url, { waitUntil: 'load' });
await page.waitForFunction(() => getComputedStyle(document.querySelector('#loader')).display === 'none', null, { timeout: 300000 });
await page.waitForTimeout(3000);
const shot = name => page.screenshot({ path: `${out}/${name}.png` });
await shot('1-title');

// HUD units (360 tall, y down) to page coordinates
const box = await page.locator('#unity-canvas').boundingBox();
const s = box.height / 360, vw = box.width / s;
const at = (x, y, id) => ({ x: box.x + x * s, y: box.y + y * s, id });
const touch = (type, points) => cdp.send('Input.dispatchTouchEvent', { type, touchPoints: points });
console.log(`canvas ${box.width.toFixed(0)}x${box.height.toFixed(0)} at ${box.x.toFixed(0)},${box.y.toFixed(0)}; ${vw.toFixed(0)} HUD units wide`);

// START sits 4 units below the middle of the title screen
const play = at(vw / 2, 180 + 4, 1);
await touch('touchStart', [play]);
await page.waitForTimeout(120);
await touch('touchEnd', []);
await page.waitForTimeout(1500);
await shot('2-playing');

// left thumb on the stick, right thumb on FIRE; then push the stick right and keep firing
const stick = [90, 280], fire = [vw - 64, 288];
await touch('touchStart', [at(stick[0], stick[1], 1), at(fire[0], fire[1], 2)]);
for (let i = 0; i <= 40; i++) {
  const push = Math.min(30, i * 3);
  await touch('touchMove', [at(stick[0] + push, stick[1], 1), at(fire[0], fire[1], 2)]);
  await page.waitForTimeout(100);
  if (i === 20) await shot('3-run-and-fire');
}
await touch('touchEnd', []);
await page.waitForTimeout(500);
await shot('4-after');

// the pause button (II), top right
const pause = at(vw - 22, 60, 3);
await touch('touchStart', [pause]);
await page.waitForTimeout(100);
await touch('touchEnd', []);
await page.waitForTimeout(800);
await shot('5-paused');

fs.writeFileSync(`${out}/console.log`, logs.join('\n') + '\n');
console.log(logs.filter(l => /PooketSlug|pageerror|error|exception/i.test(l)).slice(-40).join('\n'));
await browser.close();
