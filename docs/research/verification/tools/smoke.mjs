// Loads the WebGL build, plays a few seconds with scripted input and takes screenshots.
// usage: PW_PATH=$(npm root -g)/playwright node smoke.mjs <url> <outdir>   (PW_PATH optional if playwright is a local dependency)
import { createRequire } from 'module';
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PW_PATH || 'playwright');

const [url, out] = process.argv.slice(2);
const browser = await chromium.launch({
  args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--autoplay-policy=no-user-gesture-required'],
});
const page = await browser.newPage({ viewport: { width: 1100, height: 900 } });
const logs = [];
page.on('console', m => logs.push(`[${m.type()}] ${m.text()}`));
page.on('pageerror', e => logs.push(`[pageerror] ${e.message}`));
page.on('requestfailed', r => logs.push(`[requestfailed] ${r.url()} ${r.failure()?.errorText}`));

await page.goto(url, { waitUntil: 'load' });
const t0 = Date.now();
try {
  await page.waitForFunction(() => getComputedStyle(document.querySelector('#loader')).display === 'none', null, { timeout: 180000 });
  console.log(`loaded in ${((Date.now() - t0) / 1000).toFixed(1)}s`);
} catch (e) {
  console.log('LOAD TIMEOUT: ' + (await page.textContent('#loader')));
}
await page.waitForTimeout(3000);
await page.screenshot({ path: `${out}/1-title.png` });

const stage = page.locator('#unity-canvas');
await stage.click();
await page.keyboard.press('Enter');
await page.waitForTimeout(1500);
await page.screenshot({ path: `${out}/2-start.png` });

// run right while shooting, jump occasionally
await page.keyboard.down('ArrowRight');
for (let i = 0; i < 12; i++) {
  await page.keyboard.down('x'); await page.waitForTimeout(90); await page.keyboard.up('x');
  if (i % 4 === 1) { await page.keyboard.down('z'); await page.waitForTimeout(200); await page.keyboard.up('z'); }
  await page.waitForTimeout(150);
}
await page.keyboard.press('c');
await page.waitForTimeout(250);
await page.screenshot({ path: `${out}/3-action.png` });
await page.keyboard.up('ArrowRight');
await page.waitForTimeout(400);
await page.screenshot({ path: `${out}/4-after.png` });

console.log(logs.filter(l => !/\[debug\]/.test(l)).slice(-60).join('\n'));
await browser.close();
