import { expect, test } from '@playwright/test';
import { ApiHelper } from '../support/api';
import { LoginPage } from '../support/pages/login.page';

test.describe('delete maintenance record', () => {
  test('keeps the record when declined and removes it when confirmed, leaving the mileage as is', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('delrec');
    await api.register(email);
    const { token } = await api.login(email);
    const vehicle = await api.createVehicle(token, { currentMileage: 45000 });
    const { record } = await api.createRecord(token, vehicle.id, { description: 'Brake pads', mileageAtService: 47000 });
    await new LoginPage(page).login(email);
    await page.goto(`/vehicles/${vehicle.id}`);
    const row = page.getByTestId(`record-row-${record.id}`);
    await expect(row).toBeVisible();
    await expect(page.getByTestId('vehicle-mileage')).toHaveText('47000');

    await page.getByTestId(`delete-record-${record.id}`).click();
    await expect(page.getByTestId('confirm-delete-message')).toContainText('Brake pads');
    await page.getByTestId('confirm-keep').click();
    await expect(page.getByTestId('confirm-delete-message')).toHaveCount(0);
    await expect(row).toBeVisible();

    await page.getByTestId(`delete-record-${record.id}`).click();
    await page.getByTestId('confirm-delete').click();
    await expect(row).toHaveCount(0);
    await expect(page.getByTestId('history-empty')).toBeVisible();
    await expect(page.getByTestId('vehicle-mileage')).toHaveText('47000');
  });
});
