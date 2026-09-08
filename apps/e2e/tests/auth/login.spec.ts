import { expect, test } from '@playwright/test';
import { ApiHelper } from '../support/api';
import { LoginPage } from '../support/pages/login.page';

test.describe('log in', () => {
  test('rejects a wrong password with the API message', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('wrongpw');
    await api.register(email);

    await new LoginPage(page).attempt(email, 'Wrong99999');

    await expect(page.getByTestId('form-error')).toContainText('Invalid email or password');
    await expect(page).toHaveURL(/\/login/);
  });

  test('a fresh account lands on the vehicles page and sees the verification banner', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('fresh');
    await api.register(email);

    await new LoginPage(page).login(email);

    await expect(page).toHaveURL(/\/vehicles/);
    await expect(page.getByTestId('vehicles-heading')).toBeVisible();
    await expect(page.getByTestId('signed-in-as')).toHaveText(email);
    await expect(page.getByTestId('verification-banner')).toBeVisible();
  });

  test('resend from the banner sends a newer verification link', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('resend');
    await api.register(email);
    const firstToken = await api.latestToken(email);
    await new LoginPage(page).login(email);

    await page.getByRole('button', { name: 'Resend verification email' }).click();

    await expect(page.getByTestId('resend-confirmation')).toBeVisible();
    expect(await api.latestToken(email)).not.toBe(firstToken);
  });

  test('log out returns to the login page and protects the vehicles page again', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('logout');
    await api.register(email);
    await new LoginPage(page).login(email);
    await expect(page).toHaveURL(/\/vehicles/);

    await page.getByTestId('log-out').click();
    await expect(page).toHaveURL(/\/login/);

    await page.goto('/vehicles');
    await expect(page).toHaveURL(/\/login\?returnUrl=%2Fvehicles/);
  });
});
