import { expect, Page, test } from '@playwright/test';
import { ApiHelper } from '../support/api';
import { LoginPage } from '../support/pages/login.page';

async function fillRecord(page: Page, record: { description: string; cost: string; date: string; mileage: string; provider?: string }) {
  await page.getByLabel('Description').fill(record.description);
  await page.getByLabel('Cost (USD)').fill(record.cost);
  await page.getByLabel('Date performed').fill(record.date);
  await page.getByLabel('Mileage at service').fill(record.mileage);
  if (record.provider !== undefined) {
    await page.getByLabel('Service provider').fill(record.provider);
  }
}

test.describe('maintenance form', () => {
  test('logs a job and advances the vehicle mileage', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('logjob');
    await api.register(email);
    const { token } = await api.login(email);
    const vehicle = await api.createVehicle(token, { currentMileage: 45000 });
    await new LoginPage(page).login(email);
    await page.goto(`/vehicles/${vehicle.id}`);
    await expect(page.getByTestId('vehicle-mileage')).toHaveText('45000');

    await page.getByTestId('add-record').click();
    await fillRecord(page, { description: 'Timing belt', cost: '450.5', date: '2026-06-10', mileage: '48000', provider: 'Main Street Garage' });
    await page.getByTestId('record-submit').click();

    const row = page.locator('[data-testid^="record-row-"]').first();
    await expect(row).toContainText('Timing belt');
    await expect(row).toContainText('$450.50');
    await expect(row).toContainText('Main Street Garage');
    await expect(page.getByTestId('vehicle-mileage')).toHaveText('48000');
  });

  test('edits a job in place without lowering the vehicle mileage', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('editjob');
    await api.register(email);
    const { token } = await api.login(email);
    const vehicle = await api.createVehicle(token, { currentMileage: 45000 });
    const { record } = await api.createRecord(token, vehicle.id, { description: 'Oil change', mileageAtService: 46000 });
    await new LoginPage(page).login(email);
    await page.goto(`/vehicles/${vehicle.id}`);

    await page.getByTestId(`edit-record-${record.id}`).click();
    await page.getByLabel('Description').fill('Oil and filter change');
    await page.getByLabel('Mileage at service').fill('45500');
    await page.getByTestId('record-submit').click();

    await expect(page.getByTestId(`record-row-${record.id}`)).toContainText('Oil and filter change');
    await expect(page.getByTestId(`record-row-${record.id}`)).toContainText('45500');
    await expect(page.getByTestId('vehicle-mileage')).toHaveText('46000');
  });

  test('rejects a future date on the field', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('futurejob');
    await api.register(email);
    const { token } = await api.login(email);
    const vehicle = await api.createVehicle(token);
    await new LoginPage(page).login(email);
    await page.goto(`/vehicles/${vehicle.id}`);

    await page.getByTestId('add-record').click();
    await fillRecord(page, { description: 'Crystal ball service', cost: '10', date: '2999-01-01', mileage: '1' });
    await page.getByTestId('record-submit').click();

    await expect(page.getByTestId('datePerformed-error')).toContainText('future');
    await expect(page.getByRole('dialog', { name: 'Log a maintenance job' })).toBeVisible();
  });
});
