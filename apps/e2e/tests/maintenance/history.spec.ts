import { expect, test } from '@playwright/test';
import { ApiHelper } from '../support/api';
import { LoginPage } from '../support/pages/login.page';

test.describe('maintenance history', () => {
  test('opens a vehicle from the list and shows its records newest first', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('history');
    await api.register(email);
    const { token } = await api.login(email);
    const vehicle = await api.createVehicle(token, { make: 'Honda', model: 'Civic', currentMileage: 40000 });
    await api.createRecord(token, vehicle.id, { description: 'Brake pads', datePerformed: '2026-03-15', mileageAtService: 42000 });
    await api.createRecord(token, vehicle.id, { description: 'Oil change', datePerformed: '2026-08-20', mileageAtService: 45500, costUsd: 65 });
    await new LoginPage(page).login(email);

    await page.getByTestId(`open-${vehicle.vin}`).click();

    await expect(page).toHaveURL(new RegExp(`/vehicles/${vehicle.id}$`));
    await expect(page.getByTestId('vehicle-title')).toContainText('Honda Civic');
    await expect(page.getByTestId('vehicle-mileage')).toHaveText('45500');
    const rows = page.locator('[data-testid^="record-row-"]');
    await expect(rows).toHaveCount(2);
    await expect(rows.nth(0)).toContainText('Oil change');
    await expect(rows.nth(0)).toContainText('$65.00');
    await expect(rows.nth(1)).toContainText('Brake pads');
  });

  test('shows the empty state for a vehicle without records', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('nohistory');
    await api.register(email);
    const { token } = await api.login(email);
    const vehicle = await api.createVehicle(token);
    await new LoginPage(page).login(email);

    await page.goto(`/vehicles/${vehicle.id}`);

    await expect(page.getByTestId('history-empty')).toBeVisible();
    await page.getByTestId('back-to-vehicles').click();
    await expect(page).toHaveURL(/\/vehicles$/);
  });
});
