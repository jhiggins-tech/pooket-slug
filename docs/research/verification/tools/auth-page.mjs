// Tests the page's sign-in and cloud saves (Assets/WebGLTemplates/PooketSlug/pooket-auth.js) in headless
// Chromium, against the Realtime Database emulator running Cardmaster's real rules. No Unity build: a small
// page stands in for the game, merging and saving as Game.cs does, and Google and Firebase Auth are faked
// by a local server that hands out the unsigned tokens the emulator accepts.
//
// usage: node auth-page.mjs <cardmaster checkout> [<out dir>]
// Needs Java 21+, the database emulator's JAR (`npx firebase setup:emulators:database` in the checkout's
// firebase/ folder), and Playwright (in the cloud sandbox: PW_PATH=$(npm root -g)/playwright).
import { spawn } from 'node:child_process';
import fs from 'node:fs';
import http from 'node:http';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, '../../../..');
const cardmaster = process.argv[2];
const out = process.argv[3] ?? fs.mkdtempSync(path.join(os.tmpdir(), 'auth-page-'));
if (!cardmaster) throw new Error('usage: node auth-page.mjs <cardmaster checkout> [<out dir>]');
const { chromium } = await import(process.env.PW_PATH ? path.join(process.env.PW_PATH, 'index.mjs') : 'playwright');

const DB = 'http://127.0.0.1:9000';
const NS = 'demo-pooket-default-rtdb';
const PORT = 8123;

// ------------------------------------------------------------------ the database emulator, with Cardmaster's rules

const jars = fs.readdirSync(path.join(os.homedir(), '.cache/firebase/emulators')).filter(f => /^firebase-database-emulator-v.*\.jar$/.test(f)).sort();
const emulator = spawn('java', ['-jar', path.join(os.homedir(), '.cache/firebase/emulators', jars.at(-1)), '--host', '127.0.0.1', '--port', '9000'], { stdio: 'ignore' });
process.on('exit', () => emulator.kill());
for (let i = 0; i < 60; i++) {
  if (await fetch(`${DB}/.json?ns=${NS}`).then(() => true, () => false)) break;
  await new Promise(r => setTimeout(r, 500));
}
const rules = await fetch(`${DB}/.settings/rules.json?ns=${NS}`, {
  method: 'PUT', headers: { Authorization: 'Bearer owner' }, body: fs.readFileSync(path.join(cardmaster, 'firebase/database.rules.json')),
});
if (!rules.ok) throw new Error(`the emulator refused the rules: ${rules.status} ${await rules.text()}`);
const admin = async p => (await fetch(`${DB}/${p}.json?ns=${NS}`, { headers: { Authorization: 'Bearer owner' } })).json();

// ------------------------------------------------------------------ the page, and fake Google and Firebase Auth

let refreshes = 0;
function tokenFor(uid, life) {
  const part = o => Buffer.from(JSON.stringify(o)).toString('base64url');
  const now = Math.floor(Date.now() / 1000);
  return `${part({ alg: 'none', typ: 'JWT' })}.${part({
    iss: 'https://securetoken.google.com/demo-pooket', aud: 'demo-pooket', iat: now, exp: now + life, auth_time: now,
    sub: uid, user_id: uid, firebase: { sign_in_provider: 'google.com', identities: {} },
  })}.`;
}

const template = fs.readFileSync(path.join(repo, 'Assets/WebGLTemplates/PooketSlug/pooket-auth.js'), 'utf8');
const page = config => `<!DOCTYPE html><html><head><meta charset="utf-8"></head><body>
<header><div id="account" hidden></div></header><canvas id="unity-canvas" tabindex="-1"></canvas>
<script>window.POOKET_CONFIG = ${JSON.stringify(config)};</script>
<script src="pooket-auth.js"></script>
<script>
// Stands in for the game: its profile, and Game.OnCloudProfile's merge-then-save (Profile.Merge).
window.messages = [];
window.profile = JSON.parse(localStorage.getItem('test.profile') || '{"version":1,"coop":false,"missions":[],"seen":[],"first":0,"ts":0}');
function keep() { localStorage.setItem('test.profile', JSON.stringify(profile)); }
function merge(o) {
  profile.coop = profile.coop || o.coop;
  for (const r of o.missions) {
    const m = profile.missions.find(x => x.mission === r.mission && x.setting === r.setting);
    if (!m) profile.missions.push(r);
    else { m.best = Math.max(m.best, r.best); m.clears = Math.max(m.clears, r.clears); }
  }
  for (const s of o.seen) if (!profile.seen.includes(s)) profile.seen.push(s);
}
window.account = {};
const game = {
  SendMessage(object, method, json) {
    messages.push([object, method, JSON.parse(json)]);
    if (method === 'OnAccount') account = JSON.parse(json);
    if (method === 'OnCloudProfile') { merge(JSON.parse(json)); window.saveGame(); }
  },
};
// CloudProfileStore.Save: this browser, then the cloud if signed in (PS_Save).
window.saveGame = () => { keep(); if (account.signedIn) PooketAuth.save(JSON.stringify(profile)); };
window.run = (best, won) => {
  let m = profile.missions.find(x => x.mission === 'mission-1' && x.setting === 0);
  if (!m) profile.missions.push(m = { mission: 'mission-1', setting: 0, best: 0, clears: 0 });
  m.best = Math.max(m.best, best); if (won) { m.clears++; profile.coop = true; }
  saveGame();
};
// The game starts (PS_AuthInit) before the page gets its unityInstance, as in a real load.
setTimeout(() => PooketAuth.gameReady(), 50);
setTimeout(() => PooketAuth.attach(game), 300);
</script></body></html>`;

const configured = {
  googleClientId: 'test.apps.googleusercontent.com', firebaseApiKey: 'test-key', firebaseDbUrl: DB, dbNamespace: NS,
  authEmulator: `http://127.0.0.1:${PORT}/auth`,
};
const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, `http://127.0.0.1:${PORT}`);
  const body = await new Promise(r => { let b = ''; req.on('data', c => (b += c)); req.on('end', () => r(b)); });
  const json = (status, o) => { res.writeHead(status, { 'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*', 'Access-Control-Allow-Headers': '*' }); res.end(JSON.stringify(o)); };
  if (req.method === 'OPTIONS') return json(204, {});
  if (url.pathname.endsWith('/accounts:signInWithIdp')) {
    const google = JSON.parse(decodeURIComponent(new URLSearchParams(JSON.parse(body).postBody).get('id_token')));
    // A short-lived first token, so every call refreshes it (the refresh path gets exercised).
    return json(200, { localId: google.sub, idToken: tokenFor(google.sub, 30), refreshToken: `refresh-${google.sub}`, expiresIn: '30', firstName: google.given_name });
  }
  if (url.pathname.endsWith('/v1/token')) {
    refreshes++;
    const uid = new URLSearchParams(body).get('refresh_token').replace(/^refresh-/, '');
    return json(200, { id_token: tokenFor(uid, 3600), refresh_token: `refresh-${uid}`, expires_in: '3600' });
  }
  if (url.pathname === '/pooket-auth.js') { res.writeHead(200, { 'Content-Type': 'text/javascript' }); return res.end(template); }
  if (url.pathname === '/off.html') { res.writeHead(200, { 'Content-Type': 'text/html' }); return res.end(page({})); }
  res.writeHead(200, { 'Content-Type': 'text/html' });
  res.end(page(configured));
});
await new Promise(r => server.listen(PORT, '127.0.0.1', r));

// ------------------------------------------------------------------ the checks

const browser = await chromium.launch();
const results = [];
const logs = [];
function check(name, ok, detail = '') {
  results.push({ name, ok: !!ok, detail });
  console.log(`${ok ? 'ok  ' : 'FAIL'} ${name}${detail ? ` (${detail})` : ''}`);
}
async function device(name) {
  const context = await browser.newContext();
  const p = await context.newPage();
  p.on('console', m => logs.push(`${name} [${m.type()}] ${m.text()}`));
  p.on('pageerror', e => logs.push(`${name} [pageerror] ${e.message}`));
  return p;
}
const settle = p => p.waitForFunction(() => PooketAuth._state().queued === 0 && !document.querySelector('.account-status')?.textContent, null, { timeout: 10000 }).then(() => p.waitForTimeout(400));
const record = () => admin('pooketSlug/players/fake-ann');
const best = (rec, setting = 'normal') => rec?.missions?.['mission-1']?.[setting];

try {
  // Signed out, no sign-in settings: the area stays hidden and the game hears sign-in is off.
  const off = await device('off');
  await off.goto(`http://127.0.0.1:${PORT}/off.html`);
  await off.waitForFunction(() => 'available' in account);
  check('without settings, the account area stays hidden', await off.locator('#account').isHidden());
  check('without settings, the game hears sign-in is off', (await off.evaluate(() => account)).available === false);

  // Device A: progress from before signing in, then signs in for the first time.
  const a = await device('A');
  await a.goto(`http://127.0.0.1:${PORT}/?fakegoogle=Ann`);
  await a.waitForFunction(() => 'available' in account);
  await a.evaluate(() => run(500, true));
  check('signed out, the game hears sign-in is available', (await a.evaluate(() => account)).available === true);
  await a.click('button.fake-google');
  await settle(a);
  let rec = await record();
  check('first sign-in makes the record with save number 1', rec?.rev === 1, `rev ${rec?.rev}`);
  check('the record holds the local progress', best(rec)?.best === 500 && best(rec)?.clears === 1 && rec.coop === true, JSON.stringify(best(rec)));
  check('the record has the required fields and the first name', rec?.xp === 0 && rec?.tokens === 0 && rec?.rank === 0 && rec?.name === 'Ann' && rec?.first === rec?.ts && rec?.version === 1);
  check('the page shows who is signed in', (await a.locator('#account').innerText()).includes('Signed in as Ann'));
  check('the game hears it is signed in, before the cloud profile', (await a.evaluate(() => messages.findIndex(m => m[1] === 'OnAccount' && m[2].signedIn) < messages.findIndex(m => m[1] === 'OnCloudProfile'))));
  check('the ID token was refreshed before it lapsed', refreshes > 0, `${refreshes} refresh(es)`);

  // Device B: other progress, signs in, and the two merge.
  const b = await device('B');
  await b.goto(`http://127.0.0.1:${PORT}/?fakegoogle=Ann`);
  await b.evaluate(() => { profile.missions.push({ mission: 'mission-1', setting: 0, best: 300, clears: 3 }, { mission: 'mission-1', setting: 1, best: 100, clears: 0 }); keep(); });
  await b.click('button.fake-google');
  await settle(b);
  rec = await record();
  check('a second device merges: the better of each field, saved as save number 2', rec?.rev === 2 && best(rec).best === 500 && best(rec).clears === 3 && best(rec, 'hard')?.best === 100, `rev ${rec?.rev}, ${JSON.stringify(rec?.missions)}`);
  check('the second device got the cloud progress', (await b.evaluate(() => profile.missions.find(m => m.setting === 0).best)) === 500);

  // Device A is now behind (it last wrote save 1). Its next save is refused, re-read, merged, and saved.
  await a.evaluate(() => run(600, true));
  await settle(a);
  rec = await record();
  check('a device that was behind merges and saves after a refusal', rec?.rev === 3 && best(rec).best === 600 && best(rec).clears === 3 && best(rec, 'hard')?.best === 100, `rev ${rec?.rev}, ${JSON.stringify(rec?.missions)}`);
  check('and it picked up the other device\'s hard-mode record', (await a.evaluate(() => profile.missions.some(m => m.setting === 1))));

  // Reloading keeps the session (localStorage), and the game hears it at once.
  await a.reload();
  await settle(a);
  const first = await a.evaluate(() => messages[0]);
  check('after a reload, still signed in, and that is the game\'s first message', first?.[1] === 'OnAccount' && first[2].signedIn === true);
  rec = await record();
  check('the reload\'s merge saved once more without losing anything', rec?.rev === 4 && best(rec).best === 600, `rev ${rec?.rev}`);

  // Storage keys are this game's own (Pooket Tabks shares the origin).
  const keys = await a.evaluate(() => Object.keys(localStorage).filter(k => k !== 'test.profile'));
  check('localStorage keys start with pooket-slug.', keys.length > 0 && keys.every(k => k.startsWith('pooket-slug.')), keys.join(', '));

  // Signing out: the game hears it, and saves stay local.
  await a.click('#account button');
  await a.waitForTimeout(300);
  check('signing out tells the game', (await a.evaluate(() => account.signedIn)) === false);
  await a.evaluate(() => run(900, false));
  await a.waitForTimeout(600);
  rec = await record();
  check('signed out, a save stays on the device', rec?.rev === 4 && best(rec).best === 600, `rev ${rec?.rev}`);
  check('signing out shows the button again', await a.locator('button.fake-google').isVisible());

  // Signing out in one tab signs the other tab of the same browser out too.
  const a2 = await a.context().newPage();
  await a2.goto(`http://127.0.0.1:${PORT}/?fakegoogle=Ann`);
  await a2.click('button.fake-google');
  await settle(a2);
  await a.waitForFunction(() => account.signedIn === true, null, { timeout: 5000 }).catch(() => {});
  check('a sign-in in another tab reaches this one', (await a.evaluate(() => account.signedIn)) === true);

  // Someone else's record is out of reach.
  const other = await (await fetch(`${DB}/pooketSlug/players/fake-ann.json?ns=${NS}&auth=${tokenFor('fake-bob', 3600)}`)).status;
  check('another player cannot read the record', other === 401, `HTTP ${other}`);

  check('no page errors', !logs.some(l => l.includes('[pageerror]')), logs.filter(l => l.includes('[pageerror]')).join('; '));
} catch (e) {
  check('the test ran to the end', false, e.message);
} finally {
  fs.mkdirSync(out, { recursive: true });
  fs.writeFileSync(path.join(out, 'console.log'), logs.join('\n') + '\n');
  fs.writeFileSync(path.join(out, 'result.json'), JSON.stringify(results, null, 2));
  await browser.close();
  server.close();
  emulator.kill();
}
const failed = results.filter(r => !r.ok).length;
console.log(`${results.length - failed} of ${results.length} checks passed; logs in ${out}`);
process.exit(failed ? 1 : 0);
