import { expect, Page, test } from '@playwright/test';
import { ApiHelper } from '../support/api';
import { LoginPage } from '../support/pages/login.page';

async function fillVehicle(page: Page, vehicle: { make: string; model: string; year: string; vin: string; plate: string; mileage: string }) {
  await page.getByLabel('Make').fill(vehicle.make);
  await page.getByLabel('Model').fill(vehicle.model);
  await page.getByLabel('Year').fill(vehicle.year);
  await page.getByLabel('VIN').fill(vehicle.vin);
  await page.getByLabel('License plate').fill(vehicle.plate);
  await page.getByLabel('Current mileage').fill(vehicle.mileage);
}

test.describe('vehicle form', () => {
  test('adds a vehicle and shows it in the list', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('addveh');
    await api.register(email);
    await new LoginPage(page).login(email);
    const vin = `ADD${Date.now().toString().slice(-10)}`;

    await page.getByTestId('add-vehicle').click();
    await fillVehicle(page, { make: 'Honda', model: 'Civic', year: '2019', vin: vin.toLowerCase(), plate: 'hnd-001', mileage: '12000' });
    await page.getByTestId('vehicle-submit').click();

    await expect(page.getByTestId(`vehicle-row-${vin}`)).toContainText('Civic');
    await expect(page.getByTestId(`vehicle-row-${vin}`)).toContainText('HND-001');
  });

  test('edits a vehicle in place', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('editveh');
    await api.register(email);
    const { token } = await api.login(email);
    const vehicle = await api.createVehicle(token, { model: 'Corolla', currentMileage: 45000 });
    await new LoginPage(page).login(email);

    await page.getByTestId(`edit-${vehicle.vin}`).click();
    await page.getByLabel('Model').fill('Camry');
    await page.getByLabel('Current mileage').fill('50000');
    await page.getByTestId('vehicle-submit').click();

    await expect(page.getByTestId(`vehicle-row-${vehicle.vin}`)).toContainText('Camry');
    await expect(page.getByTestId(`vehicle-row-${vehicle.vin}`)).toContainText('50000');
  });

  test('shows the duplicate VIN message', async ({ page, request }) => {
    const api = new ApiHelper(request);
    const email = ApiHelper.uniqueEmail('dupveh');
    await api.register(email);
    const { token } = await api.login(email);
    const existing = await api.createVehicle(token);
    await new LoginPage(page).login(email);

    await page.getByTestId('add-vehicle').click();
    await fillVehicle(page, { make: 'Ford', model: 'Focus', year: '2015', vin: existing.vin, plate: 'FRD-002', mileage: '1' });
    await page.getByTestId('vehicle-submit').click();

    await expect(page.getByTestId('form-error')).toContainText('already registered');
  });
});
