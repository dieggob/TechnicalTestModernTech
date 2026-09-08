import { expect, test } from '@playwright/test';

test('the shell renders the application title', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByTestId('app-title')).toContainText('Vehicle Maintenance Tracker');
});
