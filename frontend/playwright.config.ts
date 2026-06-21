import { defineConfig, devices } from '@playwright/test';

/**
 * E2E config. The suite expects the full stack running locally:
 *   docker compose up -d          (Postgres + ETL)
 *   dotnet run --project ...Api    (API on :5180)
 *   ng serve                       (front on :4200)
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  workers: 1,
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:4200',
    locale: 'en-US',
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
