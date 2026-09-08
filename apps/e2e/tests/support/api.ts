import { APIRequestContext, expect } from '@playwright/test';
import { apiBaseUrl } from '../../playwright.config';

export interface VehicleInput {
  make: string;
  model: string;
  year: number;
  vin: string;
  licensePlate: string;
  currentMileage: number;
}

/** Talks to the API directly for test setup and for reading the Development-only recorded emails. */
export class ApiHelper {
  constructor(private readonly request: APIRequestContext) {}

  /** A throwaway address unique to one test run. */
  static uniqueEmail(prefix = 'user'): string {
    return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 1e6)}@example.com`;
  }

  async register(email: string, password = 'Secret123'): Promise<void> {
    const response = await this.request.post(`${apiBaseUrl}/api/v1/auth/register`, { data: { email, password } });
    expect(response.status(), 'register').toBe(201);
  }

  async login(email: string, password = 'Secret123'): Promise<{ token: string; userId: string }> {
    const response = await this.request.post(`${apiBaseUrl}/api/v1/auth/login`, { data: { email, password } });
    expect(response.status(), 'login').toBe(200);
    return response.json();
  }

  /** Registers a vehicle for the account whose token is given; returns the created vehicle. */
  async createVehicle(
    token: string,
    vehicle: Partial<VehicleInput> = {},
  ): Promise<{ id: string; vin: string }> {
    const data: VehicleInput = {
      make: 'Toyota',
      model: 'Corolla',
      year: 2020,
      vin: `VIN${Date.now().toString().slice(-8)}${Math.floor(Math.random() * 1e6).toString().padStart(6, '0')}`,
      licensePlate: 'ABC-123',
      currentMileage: 45000,
      ...vehicle,
    };
    const response = await this.request.post(`${apiBaseUrl}/api/v1/vehicles`, {
      data,
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(response.status(), 'create vehicle').toBe(201);
    return response.json();
  }

  /** Newest recorded email sent to the address, from the Development-only endpoint. */
  async latestEmail(to: string): Promise<{ to: string; subject: string; link: string } | undefined> {
    const response = await this.request.get(`${apiBaseUrl}/api/v1/dev/emails`);
    expect(response.ok(), 'dev emails endpoint').toBeTruthy();
    const emails: { to: string; subject: string; link: string }[] = await response.json();
    return emails.find((email) => email.to.toLowerCase() === to.toLowerCase());
  }

  /** The token inside the newest emailed link for the address. */
  async latestToken(to: string): Promise<string> {
    const email = await this.latestEmail(to);
    expect(email, `an email to ${to}`).toBeDefined();
    const token = new URL(email!.link).searchParams.get('token');
    expect(token, 'token in link').toBeTruthy();
    return token!;
  }
}
