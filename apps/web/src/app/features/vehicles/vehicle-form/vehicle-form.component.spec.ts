import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { VehiclesFacade } from '../vehicles.facade';
import { VehicleFormComponent } from './vehicle-form.component';

describe('VehicleFormComponent', () => {
  let fixture: ComponentFixture<VehicleFormComponent>;
  let component: VehicleFormComponent;
  let create: ReturnType<typeof vi.fn>;
  let update: ReturnType<typeof vi.fn>;

  const valid = { make: 'Toyota', model: 'Corolla', year: 2020, vin: '1HGCM82633A004352', licensePlate: 'ABC-123', currentMileage: 45000 };

  beforeEach(async () => {
    create = vi.fn();
    update = vi.fn();
    await TestBed.configureTestingModule({
      imports: [VehicleFormComponent],
      providers: [{ provide: VehiclesFacade, useValue: { create, update } }],
    }).compileComponents();
    fixture = TestBed.createComponent(VehicleFormComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('vehicle', null);
    component.visible.set(true);
    await fixture.whenStable();
  });

  it('creates a vehicle, emits it, and closes', async () => {
    create.mockResolvedValue({ id: 'new', ...valid });
    const saved = vi.fn();
    component.saved.subscribe(saved);
    component.form.setValue(valid);

    await component.submit();

    expect(create).toHaveBeenCalledWith(valid);
    expect(saved).toHaveBeenCalledWith(expect.objectContaining({ id: 'new' }));
    expect(component.visible()).toBe(false);
  });

  it('prefills and updates when a vehicle is given', async () => {
    fixture.componentRef.setInput('vehicle', { id: 'v1', ...valid });
    component.visible.set(false);
    component.visible.set(true);
    await fixture.whenStable();
    expect(component.form.getRawValue().vin).toBe('1HGCM82633A004352');
    update.mockResolvedValue({ id: 'v1', ...valid, model: 'Camry' });

    component.form.controls.model.setValue('Camry');
    await component.submit();

    expect(update).toHaveBeenCalledWith('v1', { ...valid, model: 'Camry' });
    expect(create).not.toHaveBeenCalled();
  });

  it('shows the duplicate-VIN message from a 409 and keeps the dialog open', async () => {
    create.mockRejectedValue(new HttpErrorResponse({ status: 409, error: { title: 'A vehicle with this VIN is already registered.' } }));
    component.form.setValue(valid);

    await component.submit();

    expect(component.formError()).toContain('already registered');
    expect(component.visible()).toBe(true);
  });

  it('does not call the facade while the form is invalid', async () => {
    component.form.setValue({ ...valid, vin: '', currentMileage: -1 });

    await component.submit();

    expect(create).not.toHaveBeenCalled();
    expect(component.hasClientError('vin')).toBe(true);
  });
});
