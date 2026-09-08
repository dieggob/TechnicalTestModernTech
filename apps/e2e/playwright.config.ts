import { defineConfig, devices } from '@playwright/test';
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';

/**
 * Loads apps/e2e/.env into process.env without overriding values already set.
 * Kept in-line so the suite has no runtime dependency beyond Playwright.
 */
function loadDotEnv(): void {
  const file = resolve(__dirname, '.env');
  if (!existsSync(file)) {
    return;
  }
  for (const line of readFileSync(file, 'utf8').split('\n')) {
    const match = line.match(/^\s*([A-Z0-9_]+)\s*=\s*(.*?)\s*$/i);
    if (match && process.env[match[1]] === undefined) {
      process.env[match[1]] = match[2];
    }
  }
}

loadDotEnv();

export const webBaseUrl = process.env['WEB_BASE_URL'] ?? 'http://localhost:4200';
export const apiBaseUrl = process.env['API_BASE_URL'] ?? 'http://localhost:5000';

export default defineConfig({
  testDir: './tests',
  globalSetup: './tests/support/global-setup.ts',
  fullyParallel: true,
  workers: 4,
  expect: { timeout: 10_000 },
  forbidOnly: !!process.env['CI'],
  retries: 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: webBaseUrl,
    trace: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      // Uses the Google Chrome installed on the machine; no browser download needed.
      use: { ...devices['Desktop Chrome'], channel: 'chrome' },
    },
  ],
});
