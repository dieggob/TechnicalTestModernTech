import { expect, test } from '@playwright/test';
import { ApiHelper } from '../support/api';

test.describe('sign up', () => {
  test('registers a new account and confirms a verification email was sent', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('signup');

    await page.goto('/register');
    await page.getByLabel('Email').fill(email);
    await page.getByLabel('Password').fill('Secret123');
    await page.getByRole('button', { name: 'Create account' }).click();

    await expect(page.getByTestId('register-success')).toContainText('Check your email');
    const sent = await api.latestEmail(email);
    expect(sent?.link).toContain('/verify?token=');
  });

  test('shows the server error when the email is already registered', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('dup');
    await api.register(email);

    await page.goto('/register');
    await page.getByLabel('Email').fill(email);
    await page.getByLabel('Password').fill('Secret123');
    await page.getByRole('button', { name: 'Create account' }).click();

    await expect(page.getByTestId('form-error')).toContainText('already registered');
  });

  test('shows field errors for a weak password', async ({ page }) => {
    await page.goto('/register');
    await page.getByLabel('Email').fill(ApiHelper.uniqueEmail('weak'));
    await page.getByLabel('Password').fill('short');
    await page.getByRole('button', { name: 'Create account' }).click();

    await expect(page.getByTestId('password-error')).toContainText('at least 8');
  });
});
