import { Locator, Page } from '@playwright/test';

/** The login screen, shared by every journey that starts from a signed-in user. */
export class LoginPage {
  readonly formError: Locator;

  constructor(private readonly page: Page) {
    this.formError = page.getByTestId('form-error');
  }

  async goto(): Promise<void> {
    await this.page.goto('/login');
  }

  /** Fills the form and submits; resolves once the app has left the login page. */
  async login(email: string, password = 'Secret123'): Promise<void> {
    await this.goto();
    await this.page.getByLabel('Email').fill(email);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Log in' }).click();
    await this.page.waitForURL((url) => !url.pathname.endsWith('/login'));
  }

  /** Submits without waiting for a redirect, for journeys that expect the login to be rejected. */
  async attempt(email: string, password = 'Secret123'): Promise<void> {
    await this.goto();
    await this.page.getByLabel('Email').fill(email);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Log in' }).click();
  }
}
