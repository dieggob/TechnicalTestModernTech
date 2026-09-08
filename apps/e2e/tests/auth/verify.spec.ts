import { expect, test } from '@playwright/test';
import { ApiHelper } from '../support/api';
import { LoginPage } from '../support/pages/login.page';

test.describe('verify email', () => {
  test('the emailed link verifies the account and clears the banner without a new login', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('verify');
    await api.register(email);
    await new LoginPage(page).login(email);
    await expect(page.getByTestId('verification-banner')).toBeVisible();

    const token = await api.latestToken(email);
    await page.goto(`/verify?token=${encodeURIComponent(token)}`);

    await expect(page.getByTestId('verify-success')).toBeVisible();
    await expect(page.getByTestId('verification-banner')).toHaveCount(0);
  });

  test('a used link shows the invalid message', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('reuse');
    await api.register(email);
    const token = await api.latestToken(email);

    await page.goto(`/verify?token=${encodeURIComponent(token)}`);
    await expect(page.getByTestId('verify-success')).toBeVisible();

    await page.goto(`/verify?token=${encodeURIComponent(token)}`);
    await expect(page.getByTestId('verify-invalid')).toBeVisible();
  });
});
