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

  async login(email: string, password = 'Secret123'): Promise<void> {
    await this.goto();
    await this.page.getByLabel('Email').fill(email);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Log in' }).click();
  }
}
