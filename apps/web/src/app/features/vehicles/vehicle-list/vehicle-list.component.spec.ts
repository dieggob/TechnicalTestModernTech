import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { VehiclesFacade } from '../vehicles.facade';
import { VehicleListComponent } from './vehicle-list.component';

describe('VehicleListComponent', () => {
  let fixture: ComponentFixture<VehicleListComponent>;
  const facade = {
    vehicles: signal<unknown[]>([]),
    loading: signal(false),
    error: signal<string | null>(null),
    load: vi.fn().mockResolvedValue(undefined),
  };

  beforeEach(async () => {
    facade.vehicles.set([]);
    facade.error.set(null);
    facade.load.mockClear();
    await TestBed.configureTestingModule({
      imports: [VehicleListComponent],
      providers: [provideRouter([]), { provide: VehiclesFacade, useValue: facade }],
    }).compileComponents();
    fixture = TestBed.createComponent(VehicleListComponent);
    await fixture.whenStable();
  });

  it('loads the vehicles on init and shows the empty state', () => {
    expect(facade.load).toHaveBeenCalledTimes(1);
    expect(fixture.nativeElement.querySelector('[data-testid="vehicles-empty"]')).not.toBeNull();
  });

  it('renders one row per vehicle', async () => {
    facade.vehicles.set([
      { id: '1', make: 'Toyota', model: 'Corolla', year: 2020, vin: 'VIN1', licensePlate: 'ABC-123', currentMileage: 45000 },
      { id: '2', make: 'Honda', model: 'Civic', year: 2018, vin: 'VIN2', licensePlate: 'XYZ-987', currentMileage: 70000 },
    ]);
    await fixture.whenStable();

    const rows = fixture.nativeElement.querySelectorAll('[data-testid^="vehicle-row-"]');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Corolla');
    expect(fixture.nativeElement.querySelector('[data-testid="vehicles-empty"]')).toBeNull();
  });

  it('shows the facade error', async () => {
    facade.error.set('Your vehicles could not be loaded.');
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[data-testid="vehicles-error"]')?.textContent).toContain('could not be loaded');
  });
});
