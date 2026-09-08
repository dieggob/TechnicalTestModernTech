import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { MaintenanceFacade } from '../../maintenance/maintenance.facade';
import { VehiclesFacade } from '../vehicles.facade';
import { VehicleDetailComponent } from './vehicle-detail.component';

describe('VehicleDetailComponent', () => {
  let fixture: ComponentFixture<VehicleDetailComponent>;
  const maintenance = {
    records: signal<unknown[]>([]),
    loading: signal(false),
    error: signal<string | null>(null),
    load: vi.fn().mockResolvedValue(undefined),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  };
  const vehicles = { get: vi.fn() };
  const vehicle = { id: 'v1', make: 'Toyota', model: 'Corolla', year: 2020, vin: 'VIN1', licensePlate: 'ABC-123', currentMileage: 45000 };

  beforeEach(async () => {
    maintenance.records.set([]);
    maintenance.error.set(null);
    maintenance.load.mockClear();
    vehicles.get.mockResolvedValue(vehicle);
    await TestBed.configureTestingModule({
      imports: [VehicleDetailComponent],
      providers: [provideRouter([]), { provide: MaintenanceFacade, useValue: maintenance }, { provide: VehiclesFacade, useValue: vehicles }],
    }).compileComponents();
    fixture = TestBed.createComponent(VehicleDetailComponent);
    fixture.componentRef.setInput('vehicleId', 'v1');
    await fixture.whenStable();
  });

  it('loads the vehicle and its history for the route id', () => {
    expect(vehicles.get).toHaveBeenCalledWith('v1');
    expect(maintenance.load).toHaveBeenCalledWith('v1');
    expect(fixture.nativeElement.querySelector('[data-testid="vehicle-title"]')?.textContent).toContain('Corolla');
    expect(fixture.nativeElement.querySelector('[data-testid="history-empty"]')).not.toBeNull();
  });

  it('renders one row per record with formatted cost and date', async () => {
    maintenance.records.set([
      { id: 'r1', description: 'Oil change', costUsd: 89.99, datePerformed: '2026-09-01', mileageAtService: 46000, serviceProvider: 'Quick Lube', notes: null },
    ]);
    await fixture.whenStable();

    const row = fixture.nativeElement.querySelector('[data-testid="record-row-r1"]');
    expect(row?.textContent).toContain('Oil change');
    expect(row?.textContent).toContain('$89.99');
    expect(row?.textContent).toContain('Sep 1, 2026');
    expect(row?.textContent).toContain('—');
  });

  it('opens the dialog blank for a new job and prefilled for an edit', async () => {
    const record = { id: 'r1', description: 'Oil change' };
    maintenance.records.set([record]);
    await fixture.whenStable();
    const component = fixture.componentInstance;

    component.add();
    expect(component.formVisible()).toBe(true);
    expect(component.editing()).toBeNull();

    component.formVisible.set(false);
    component.edit(record);
    expect(component.formVisible()).toBe(true);
    expect(component.editing()).toBe(record);
  });

  it('applies the vehicle mileage returned by a write', async () => {
    fixture.componentInstance.onSaved({ record: { id: 'r9' }, vehicleCurrentMileage: 52000 });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[data-testid="vehicle-mileage"]')?.textContent).toBe('52000');
  });

  it('deletes a record only after confirmation', async () => {
    const record = { id: 'r1', description: 'Oil change', datePerformed: '2026-09-01' };
    maintenance.records.set([record]);
    maintenance.delete.mockResolvedValue(undefined);
    const component = fixture.componentInstance;

    component.confirmDelete(record);
    component.keep();
    expect(maintenance.delete).not.toHaveBeenCalled();

    component.confirmDelete(record);
    await component.deleteConfirmed();

    expect(maintenance.delete).toHaveBeenCalledWith('v1', 'r1');
    expect(component.deleting()).toBeNull();
  });

  it('reports a failed delete and closes the confirmation', async () => {
    const record = { id: 'r1', description: 'Oil change' };
    maintenance.delete.mockRejectedValueOnce(new Error('500'));
    const component = fixture.componentInstance;

    component.confirmDelete(record);
    await component.deleteConfirmed();
    await fixture.whenStable();

    expect(component.deleting()).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="history-action-error"]')).not.toBeNull();
  });

  it('shows an error when the vehicle cannot be loaded', async () => {
    vehicles.get.mockRejectedValueOnce(new Error('404'));
    const other = TestBed.createComponent(VehicleDetailComponent);
    other.componentRef.setInput('vehicleId', 'missing');
    await other.whenStable();

    expect(other.nativeElement.querySelector('[data-testid="vehicle-error"]')).not.toBeNull();
  });
});
