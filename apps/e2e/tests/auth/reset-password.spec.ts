import { expect, test } from '@playwright/test';
import { ApiHelper } from '../support/api';
import { LoginPage } from '../support/pages/login.page';

test.describe('reset password', () => {
  test('requests a link, sets a new password through it, and logs in with the new one', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('reset');
    await api.register(email, 'OldSecret1');

    await page.goto('/forgot-password');
    await page.getByLabel('Email').fill(email);
    await page.getByRole('button', { name: 'Send reset link' }).click();
    await expect(page.getByTestId('forgot-success')).toBeVisible();

    const token = await api.latestToken(email);
    await page.goto(`/reset-password?token=${encodeURIComponent(token)}`);
    await page.getByLabel('New password').fill('NewSecret2');
    await page.getByRole('button', { name: 'Change password' }).click();
    await expect(page.getByTestId('reset-success')).toBeVisible();

    await new LoginPage(page).login(email, 'NewSecret2');
    await expect(page).toHaveURL(/\/vehicles/);

    await page.goto(`/reset-password?token=${encodeURIComponent(token)}`);
    await page.getByLabel('New password').fill('Another3');
    await page.getByRole('button', { name: 'Change password' }).click();
    await expect(page.getByTestId('form-error')).toContainText('invalid or has expired');
  });
});
