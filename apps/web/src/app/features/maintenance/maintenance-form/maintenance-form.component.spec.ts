import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MaintenanceFacade } from '../maintenance.facade';
import { MaintenanceFormComponent, notInFuture, todayIso, twoDecimals } from './maintenance-form.component';
import { FormControl } from '@angular/forms';

describe('MaintenanceFormComponent', () => {
  let fixture: ComponentFixture<MaintenanceFormComponent>;
  let component: MaintenanceFormComponent;
  let create: ReturnType<typeof vi.fn>;
  let update: ReturnType<typeof vi.fn>;

  const valid = { description: 'Oil change', costUsd: 89.99, datePerformed: '2026-08-01', mileageAtService: 46000, serviceProvider: 'Quick Lube', notes: '' };
  const sent = { ...valid, notes: null };

  beforeEach(async () => {
    create = vi.fn();
    update = vi.fn();
    await TestBed.configureTestingModule({
      imports: [MaintenanceFormComponent],
      providers: [{ provide: MaintenanceFacade, useValue: { create, update } }],
    }).compileComponents();
    fixture = TestBed.createComponent(MaintenanceFormComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('vehicleId', 'v1');
    fixture.componentRef.setInput('record', null);
    component.visible.set(true);
    await fixture.whenStable();
  });

  it('logs a job for the vehicle, emits the result, and closes', async () => {
    const result = { record: { id: 'r1', ...sent }, vehicleCurrentMileage: 46000 };
    create.mockResolvedValue(result);
    const saved = vi.fn();
    component.saved.subscribe(saved);
    component.form.setValue(valid);

    await component.submit();

    expect(create).toHaveBeenCalledWith('v1', sent);
    expect(saved).toHaveBeenCalledWith(result);
    expect(component.visible()).toBe(false);
  });

  it('prefills and updates when a record is given', async () => {
    fixture.componentRef.setInput('record', { id: 'r1', ...sent });
    component.visible.set(false);
    component.visible.set(true);
    await fixture.whenStable();
    expect(component.form.getRawValue().description).toBe('Oil change');
    update.mockResolvedValue({ record: { id: 'r1', ...sent, costUsd: 95 }, vehicleCurrentMileage: 46000 });

    component.form.controls.costUsd.setValue(95);
    await component.submit();

    expect(update).toHaveBeenCalledWith('v1', 'r1', { ...sent, costUsd: 95 });
    expect(create).not.toHaveBeenCalled();
  });

  it('binds a 400 field error to its control and keeps the dialog open', async () => {
    create.mockRejectedValue(
      new HttpErrorResponse({ status: 400, error: { title: 'Validation failed', errors: { datePerformed: ['Date performed cannot be in the future.'] } } }),
    );
    component.form.setValue(valid);

    await component.submit();

    expect(component.serverError('datePerformed')).toContain('future');
    expect(component.formError()).toBeNull();
    expect(component.visible()).toBe(true);
  });

  it('does not call the facade while the form is invalid', async () => {
    component.form.setValue({ ...valid, description: '', costUsd: 1.005 });

    await component.submit();

    expect(create).not.toHaveBeenCalled();
    expect(component.hasClientError('description')).toBe(true);
    expect(component.hasClientError('costUsd')).toBe(true);
  });

  it('starts a new job on today\'s date', () => {
    expect(component.form.getRawValue().datePerformed).toBe(todayIso());
  });
});

describe('maintenance validators', () => {
  it('rejects tomorrow and accepts today', () => {
    const tomorrow = new Date(Date.now() + 86_400_000);
    const iso = new Date(tomorrow.getTime() - tomorrow.getTimezoneOffset() * 60_000).toISOString().slice(0, 10);
    expect(notInFuture(new FormControl(iso, { nonNullable: true }))).toEqual({ future: true });
    expect(notInFuture(new FormControl(todayIso(), { nonNullable: true }))).toBeNull();
  });

  it('allows up to two decimals', () => {
    expect(twoDecimals(new FormControl(12.34))).toBeNull();
    expect(twoDecimals(new FormControl(12.345))).toEqual({ decimals: true });
  });
});
