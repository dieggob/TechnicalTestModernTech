import { expect, test } from '@playwright/test';
import { execFileSync } from 'node:child_process';
import { resolve } from 'node:path';
import { apiBaseUrl } from '../../playwright.config';
import { LoginPage } from '../support/pages/login.page';

const repoRoot = resolve(__dirname, '../../../..');

/** Runs the seeder against the API under test; a second run must change nothing. */
function seed(): string {
  return execFileSync('node', [resolve(repoRoot, 'scripts/lib/seed.mjs'), apiBaseUrl, resolve(repoRoot, 'docs/data/test-data.json')], {
    encoding: 'utf8',
  });
}

test.describe('seed data', () => {
  test('seeds the sample accounts, vehicles, and records, and is idempotent', async ({ page }) => {
    seed();
    const second = seed();
    expect(second).toMatch(/seeded: 0 users \(0 verified\), 0 vehicles, 0 records; \d+ already present/);

    await new LoginPage(page).login('ana.torres@example.com', 'Garage2026');

    await expect(page.getByTestId('verification-banner')).toHaveCount(0);
    await expect(page.locator('[data-testid^="vehicle-row-"]')).toHaveCount(4);
    const corolla = page.getByTestId('vehicle-row-JTDBR32E720123456');
    await expect(corolla).toContainText('59800');

    await page.getByTestId('open-JTDBR32E720123456').click();
    await expect(page.getByTestId('vehicle-mileage')).toHaveText('59800');
    const rows = page.locator('[data-testid^="record-row-"]');
    await expect(rows).toHaveCount(4);
    await expect(rows.first()).toContainText('Cabin air filter');
  });

  test('a second user sees only their own vehicle', async ({ page }) => {
    seed();
    await new LoginPage(page).login('luis.mendez@example.com', 'Wrench2026');

    await expect(page.locator('[data-testid^="vehicle-row-"]')).toHaveCount(1);
    await expect(page.getByTestId('vehicle-row-JTDBR32E720123456')).toContainText('LMZ-9001');
  });
});
