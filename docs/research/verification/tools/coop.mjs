// Two-player co-op check against a deployed build, in two separate headless Chromium instances.
//   code mode (default): A hosts (H), B opens the invite link ?room=CODE and presses Enter.
//   quick mode:          both press Q (quick match).
// Then both play for a while, A leaves (Esc Esc), and B should take over the shared world.
// usage: PW_PATH=... node coop.mjs <url> <outdir> [code|quick]
import { createRequire } from 'module';
import fs from 'fs';
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PW_PATH || 'playwright');

const [url, out, mode = 'code'] = process.argv.slice(2);
fs.mkdirSync(out, { recursive: true });
const T0 = Date.now();
const t = () => ((Date.now() - T0) / 1000).toFixed(1).padStart(6);
const ARGS = ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--autoplay-policy=no-user-gesture-required'];

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

/** Hold a direction for `ms` while tapping fire (and jumping now and then). */
async function play(p, dir, ms) {
  const kb = p.page.keyboard;
  await kb.down(dir);
  const end = Date.now() + ms;
  for (let i = 0; Date.now() < end; i++) {
    await kb.down('x'); await p.page.waitForTimeout(80); await kb.up('x');
    if (i % 5 === 2) { await kb.down('z'); await p.page.waitForTimeout(180); await kb.up('z'); }
    await p.page.waitForTimeout(120);
  }
  await kb.up(dir);
}

const A = await player('A');
const B = await player('B');
try {
  await load(A, url);
  await shot(A, '0-title');

  let joinUrl = url;
  if (mode === 'quick') {
    await A.page.keyboard.press('q');
  } else {
    await A.page.keyboard.press('h');
    const line = await waitLog(A, /\[PooketSlug\] Online .*room ([A-Z0-9]+-[A-Z]+)/, 90000);
    if (!line) throw new Error('A never got a room code');
    const code = line.match(/room ([A-Z0-9]+-[A-Z]+)/)[1];
    console.log(`${t()} A hosts ${code}`);
    joinUrl = url + (url.includes('?') ? '&' : '?') + 'room=' + code;
  }
  await A.page.waitForTimeout(3000);
  await shot(A, '1-online');

  await load(B, joinUrl);
  await shot(B, '0-title');
  await B.page.keyboard.press(mode === 'quick' ? 'q' : 'Enter');
  if (!(await waitLog(B, /\[PooketSlug\] Online /, 90000))) throw new Error('B never got online');
  await waitLog(A, /\[PooketSlug\] .* joined/, 30000);
  await B.page.waitForTimeout(4000);
  await Promise.all([shot(A, '2-together'), shot(B, '2-together')]);

  // both advance and shoot
  await Promise.all([play(A, 'ArrowRight', 7000), play(B, 'ArrowRight', 7000)]);
  await Promise.all([shot(A, '3-fighting'), shot(B, '3-fighting')]);
  await Promise.all([play(A, 'ArrowRight', 6000), play(B, 'ArrowRight', 6000)]);
  await Promise.all([shot(A, '4-further'), shot(B, '4-further')]);

  // the master client leaves: B should inherit the shared world and carry on alone
  await A.page.keyboard.press('Escape');
  await A.page.waitForTimeout(300);
  await A.page.keyboard.press('Escape');
  const takeover = await waitLog(B, /now runs the shared world/, 30000);
  console.log(`${t()} takeover: ${takeover ? 'yes' : 'NO'}`);
  await play(B, 'ArrowRight', 4000);
  await Promise.all([shot(A, '5-left'), shot(B, '5-alone')]);
} catch (e) {
  console.log(`${t()} FAILED: ${e.message}`);
  await Promise.all([shot(A, 'x-fail').catch(() => {}), shot(B, 'x-fail').catch(() => {})]);
} finally {
  const keep = l => !/\[debug\]/.test(l);
  const all = [...A.logs, ...B.logs].filter(keep).sort((a, b) => parseFloat(a) - parseFloat(b));
  fs.writeFileSync(`${out}/console.log`, all.join('\n') + '\n');
  console.log(all.filter(l => /PooketSlug|error|exception|warn|Fusion|Photon/i.test(l)).slice(-120).join('\n'));
  await A.browser.close();
  await B.browser.close();
}
