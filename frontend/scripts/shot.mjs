import { chromium } from 'playwright';

const BASE = process.env.BASE ?? 'http://localhost:4200';
const OUT = process.env.OUT ?? 'shots';
const widths = [390, 768, 1280];

const browser = await chromium.launch();

async function shot(page, name) {
  await page.waitForTimeout(900);
  await page.screenshot({ path: `${OUT}/${name}.png`, fullPage: true });
  console.log('shot', name);
}

for (const w of widths) {
  const ctx = await browser.newContext({ viewport: { width: w, height: 850 }, deviceScaleFactor: 1 });
  const page = await ctx.newPage();

  // login
  await page.goto(`${BASE}/login`, { waitUntil: 'networkidle' });
  await shot(page, `login-${w}`);

  // log in as owner via demo button, land on dashboards
  await page.getByText('admin@pulseboard.io').click();
  await page.waitForURL('**/dashboards', { timeout: 15000 });
  await page.waitForLoadState('networkidle');
  await shot(page, `dashboards-${w}`);

  // open the seeded dashboard
  await page.getByText('Open builder').first().click().catch(() => {});
  await page.waitForLoadState('networkidle');
  await shot(page, `board-${w}`);

  // datasets
  await page.goto(`${BASE}/datasets`, { waitUntil: 'networkidle' });
  await shot(page, `datasets-${w}`);

  await ctx.close();
}

await browser.close();
console.log('done');
