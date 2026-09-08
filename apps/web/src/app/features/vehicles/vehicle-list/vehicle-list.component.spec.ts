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
    delete: vi.fn().mockResolvedValue(undefined),
  };

  beforeEach(async () => {
    facade.vehicles.set([]);
    facade.error.set(null);
    facade.load.mockClear();
    facade.delete.mockClear();
    await TestBed.configureTestingModule({
      imports: [VehicleListComponent],
      providers: [provideRouter([]), { provide: VehiclesFacade, useValue: facade }],
    }).compileComponents();
    fixture = TestBed.createComponent(VehicleListComponent);
    await fixture.whenStable();
  });

  const vehicle = { id: '1', make: 'Toyota', model: 'Corolla', year: 2020, vin: 'VIN1', licensePlate: 'ABC-123', currentMileage: 45000 };

  it('loads the vehicles on init and shows the empty state', () => {
    expect(facade.load).toHaveBeenCalledTimes(1);
    expect(fixture.nativeElement.querySelector('[data-testid="vehicles-empty"]')).not.toBeNull();
  });

  it('renders one row per vehicle', async () => {
    facade.vehicles.set([vehicle, { ...vehicle, id: '2', vin: 'VIN2', model: 'Civic' }]);
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

  it('asks before deleting, keeps on decline, and deletes on confirm', async () => {
    const component = fixture.componentInstance;

    component.confirmDelete(vehicle);
    expect(component.deleting()).toEqual(vehicle);
    expect(facade.delete).not.toHaveBeenCalled();

    component.keep();
    expect(component.deleting()).toBeNull();

    component.confirmDelete(vehicle);
    await component.deleteConfirmed();
    expect(facade.delete).toHaveBeenCalledWith('1');
    expect(component.deleting()).toBeNull();
  });

  it('reports a failed deletion', async () => {
    facade.delete.mockRejectedValueOnce(new Error('boom'));
    const component = fixture.componentInstance;

    component.confirmDelete(vehicle);
    await component.deleteConfirmed();

    expect(component.actionError()).toContain('could not be deleted');
    expect(component.deleting()).toBeNull();
  });
});
