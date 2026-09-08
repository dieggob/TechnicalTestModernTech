import { expect, test } from '@playwright/test';
import { ApiHelper } from '../support/api';
import { LoginPage } from '../support/pages/login.page';

test.describe('vehicle list', () => {
  test('shows the empty state for a new account', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('novehicles');
    await api.register(email);

    await new LoginPage(page).login(email);

    await expect(page.getByTestId('vehicles-empty')).toBeVisible();
  });

  test('lists the vehicles registered through the API', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('listed');
    await api.register(email);
    const { token } = await api.login(email);
    const first = await api.createVehicle(token, { make: 'Honda', model: 'Civic' });
    const second = await api.createVehicle(token, { make: 'Toyota', model: 'Corolla' });

    await new LoginPage(page).login(email);

    await expect(page.getByTestId(`vehicle-row-${first.vin}`)).toContainText('Civic');
    await expect(page.getByTestId(`vehicle-row-${second.vin}`)).toContainText('Corolla');
    await expect(page.getByTestId('vehicles-empty')).toHaveCount(0);
  });
});
