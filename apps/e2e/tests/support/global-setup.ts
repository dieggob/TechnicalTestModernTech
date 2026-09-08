import { chromium, request as playwrightRequest } from '@playwright/test';
import { apiBaseUrl, webBaseUrl } from '../../playwright.config';
import { ApiHelper } from './api';

/**
 * Warms the client before the suite: the Angular dev server optimises dependencies of lazy
 * routes on first use and reloads the page when it does, which breaks journeys running in
 * parallel. One login that reaches the vehicles page triggers that once, up front.
 */
export default async function globalSetup(): Promise<void> {
  const request = await playwrightRequest.newContext();
  const api = new ApiHelper(request);
  const email = ApiHelper.uniqueEmail('warmup');
  await api.register(email);

  const browser = await chromium.launch({ channel: 'chrome' });
  const page = await browser.newPage({ baseURL: webBaseUrl });
  try {
    await page.goto('/login');
    await page.getByLabel('Email').fill(email);
    await page.getByLabel('Password').fill('Secret123');
    await page.getByRole('button', { name: 'Log in' }).click();
    await page.getByTestId('vehicles-heading').waitFor({ timeout: 60_000 });
    await page.getByTestId('add-vehicle').click();
    await page.getByRole('dialog').waitFor({ timeout: 30_000 });
  } finally {
    await browser.close();
    await request.dispose();
  }
  console.log(`warm-up done against ${webBaseUrl} and ${apiBaseUrl}`);
}
