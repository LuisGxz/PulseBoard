import { expect, Page, test } from '@playwright/test';

/** Console/page errors that are environmental noise rather than app bugs. */
const BENIGN = [/ResizeObserver loop/i, /favicon/i, /Failed to load resource.*favicon/i];

/** Attaches console + pageerror collectors and returns the (filtered) error list. */
function watchErrors(page: Page): string[] {
  const errors: string[] = [];
  page.on('console', msg => {
    if (msg.type() === 'error' && !BENIGN.some(rx => rx.test(msg.text()))) errors.push(msg.text());
  });
  page.on('pageerror', err => {
    if (!BENIGN.some(rx => rx.test(err.message))) errors.push(err.message);
  });
  return errors;
}

async function loginAsOwner(page: Page) {
  await page.addInitScript(() => localStorage.setItem('pulseboard.tourSeen', '1'));
  await page.goto('/login');
  await page.getByText('admin@pulseboard.io').click();
  await page.waitForURL('**/dashboards');
}

test.describe('PulseBoard end-to-end', () => {
  test('signs in and lists dashboards without console errors', async ({ page }) => {
    const errors = watchErrors(page);
    await loginAsOwner(page);
    await expect(page.getByRole('heading', { name: 'Dashboards' })).toBeVisible();
    await expect(page.getByText('Product revenue · Q2')).toBeVisible();
    expect(errors).toEqual([]);
  });

  test('renders every widget type on the board', async ({ page }) => {
    const errors = watchErrors(page);
    await loginAsOwner(page);
    await page.locator('a[href^="/dashboards/"]').first().click();
    await page.waitForURL(/dashboards\/.+/);

    // KPIs computed
    await expect(page.getByText('Revenue', { exact: true })).toBeVisible();
    await expect(page.getByText('Paid conversion')).toBeVisible();

    // Charts: ApexCharts canvases rendered (line + donut + bar + heatmap = 4)
    await expect(page.locator('.apexcharts-canvas').first()).toBeVisible({ timeout: 15_000 });
    expect(await page.locator('.apexcharts-canvas').count()).toBeGreaterThanOrEqual(4);
    expect(errors).toEqual([]);
  });

  test('builder creates and deletes a widget', async ({ page }) => {
    const errors = watchErrors(page);
    await loginAsOwner(page);
    await page.locator('a[href^="/dashboards/"]').first().click();
    await page.waitForURL(/dashboards\/.+/);
    await page.locator('.apexcharts-canvas').first().waitFor({ timeout: 15_000 });

    const before = await page.locator('[data-tour=grid] > div').count();

    await page.getByRole('button', { name: 'Edit' }).click();
    await page.getByRole('button', { name: 'Add widget' }).click();
    await expect(page.getByRole('heading', { name: 'Add widget' })).toBeVisible();

    // live preview renders a chart
    await expect(page.locator('.apexcharts-canvas').last()).toBeVisible({ timeout: 15_000 });

    await page.locator('input.input').first().fill('E2E widget');
    await page.locator('footer').getByRole('button', { name: 'Save' }).click();

    await expect(page.locator('[data-tour=grid] > div')).toHaveCount(before + 1);

    page.once('dialog', d => d.accept());
    await page.locator('[data-tour=grid] > div', { hasText: 'E2E widget' })
      .getByRole('button', { name: 'Delete widget' }).click();
    await expect(page.locator('[data-tour=grid] > div')).toHaveCount(before);

    expect(errors).toEqual([]);
  });

  test('explores a dataset table and filters it', async ({ page }) => {
    const errors = watchErrors(page);
    await loginAsOwner(page);
    await page.goto('/datasets');
    await page.locator('a[href^="/datasets/"]').first().click();
    await page.waitForURL(/datasets\/.+/);

    await expect(page.locator('table tbody tr').first()).toBeVisible();
    const fullCount = await page.locator('table tbody tr').count();
    expect(fullCount).toBeGreaterThan(0);

    // add a filter region = Europe, apply
    await page.getByRole('button', { name: 'Add' }).click();
    await page.locator('select').first().selectOption({ label: 'Region' });
    await page.locator('input.input').last().fill('Europe');
    await page.getByRole('button', { name: 'Apply' }).click();
    await expect(page.locator('table tbody tr').first()).toBeVisible();

    expect(errors).toEqual([]);
  });

  test('toggles language to Spanish', async ({ page }) => {
    const errors = watchErrors(page);
    await loginAsOwner(page);
    await page.getByRole('button', { name: 'ES' }).click();
    await expect(page.getByRole('heading', { name: 'Tableros' })).toBeVisible();
    expect(errors).toEqual([]);
  });
});
