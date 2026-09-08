import { expect, test } from '@playwright/test';
import { ApiHelper } from '../support/api';
import { LoginPage } from '../support/pages/login.page';

test.describe('delete vehicle', () => {
  test('keeps the vehicle when the confirmation is declined and removes it when accepted', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('delveh');
    await api.register(email);
    const { token } = await api.login(email);
    const vehicle = await api.createVehicle(token);
    await new LoginPage(page).login(email);
    const row = page.getByTestId(`vehicle-row-${vehicle.vin}`);
    await expect(row).toBeVisible();

    await page.getByTestId(`delete-${vehicle.vin}`).click();
    await expect(page.getByTestId('confirm-delete-message')).toContainText('maintenance history');
    await page.getByTestId('confirm-keep').click();
    await expect(page.getByTestId('confirm-delete-message')).toHaveCount(0);
    await expect(row).toBeVisible();

    await page.getByTestId(`delete-${vehicle.vin}`).click();
    await page.getByTestId('confirm-delete').click();
    await expect(row).toHaveCount(0);
    await expect(page.getByTestId('vehicles-empty')).toBeVisible();
  });
});
