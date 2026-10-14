// Render doc pages behind a JS challenge and dump innerText (+ links) to files.
// usage: PW_PATH=$(npm root -g)/playwright node fetch-docs.mjs <outdir> <url>...
import { createRequire } from 'module';
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PW_PATH || "playwright");
const [out, ...urls] = process.argv.slice(2);
const fs = require('fs');
const browser = await chromium.launch();
const ctx = await browser.newContext({ userAgent: 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0 Safari/537.36' });
for (const url of urls) {
  const page = await ctx.newPage();
  try {
    await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60000 });
    for (let i = 0; i < 20; i++) {
      const t = await page.title();
      if (!/validat|just a moment|checking/i.test(t) && (await page.evaluate(() => document.body.innerText.length)) > 800) break;
      await page.waitForTimeout(1500);
    }
    await page.waitForTimeout(1000);
    const text = await page.evaluate(() => (document.querySelector('main, article, .content, #content') || document.body).innerText);
    const links = await page.evaluate(() => [...document.querySelectorAll('a[href]')].map(a => a.href).filter(h => /photonengine|github|unity/.test(h)));
    const name = url.replace(/^https?:\/\//, '').replace(/[^a-z0-9]+/gi, '_').slice(0, 120);
    fs.writeFileSync(`${out}/${name}.txt`, `URL: ${url}\nTITLE: ${await page.title()}\n\n${text}\n\nLINKS:\n${[...new Set(links)].join('\n')}`);
    console.log(`${name}: ${text.length} chars, title=${await page.title()}`);
  } catch (e) { console.log(`FAIL ${url}: ${e.message}`); }
  await page.close();
}
await browser.close();
