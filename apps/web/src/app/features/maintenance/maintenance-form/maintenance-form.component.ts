import { Component, effect, inject, input, model, output, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TextareaModule } from 'primeng/textarea';
import { MaintenanceInput } from '../../../api/models/maintenance-input';
import { MaintenanceRecordDto } from '../../../api/models/maintenance-record-dto';
import { MaintenanceWriteResult } from '../../../api/models/maintenance-write-result';
import { applyProblemDetails, serverError } from '../../../shared/problem-details';
import { MaintenanceFacade } from '../maintenance.facade';

type MaintenanceField = keyof MaintenanceInput;

/** Today's date in the browser's zone as an ISO date, the format `<input type="date">` uses. */
export function todayIso(): string {
  const now = new Date();
  return new Date(now.getTime() - now.getTimezoneOffset() * 60_000).toISOString().slice(0, 10);
}

/** Mirrors the API rule: the date performed cannot be in the future. */
export function notInFuture(control: AbstractControl<string>): ValidationErrors | null {
  return control.value && control.value > todayIso() ? { future: true } : null;
}

/** Mirrors the API rule: at most two decimals. */
export function twoDecimals(control: AbstractControl<number | null>): ValidationErrors | null {
  const value = control.value;
  return value !== null && value !== undefined && Math.round(value * 100) !== value * 100 ? { decimals: true } : null;
}

/**
 * One dialog for both logging and editing a maintenance job: the mode follows whether a record
 * is passed in. Server field errors land on the matching controls through the shared mapper.
 */
@Component({
  selector: 'app-maintenance-form',
  imports: [ReactiveFormsModule, ButtonModule, DialogModule, InputTextModule, MessageModule, TextareaModule],
  templateUrl: './maintenance-form.component.html',
})
export class MaintenanceFormComponent {
  private readonly facade = inject(MaintenanceFacade);
  private readonly formBuilder = inject(FormBuilder);

  /** Two-way bound by the host: opening resets the form to the record being edited, or blank. */
  readonly visible = model(false);
  readonly vehicleId = input.required<string>();
  readonly record = input<MaintenanceRecordDto | null>(null);
  /** The API's result, including the vehicle's mileage after the write. */
  readonly saved = output<MaintenanceWriteResult>();

  readonly form = this.formBuilder.nonNullable.group({
    description: ['', [Validators.required, Validators.maxLength(200)]],
    costUsd: [0, [Validators.required, Validators.min(0), twoDecimals]],
    datePerformed: [todayIso(), [Validators.required, notInFuture]],
    mileageAtService: [0, [Validators.required, Validators.min(0)]],
    serviceProvider: ['', [Validators.maxLength(150)]],
    notes: ['', [Validators.maxLength(2000)]],
  });

  readonly submitting = signal(false);
  readonly formError = signal<string | null>(null);

  constructor() {
    effect(() => {
      if (this.visible()) {
        this.reset(this.record());
      }
    });
  }

  get editing(): boolean {
    return this.record() !== null;
  }

  serverError(field: MaintenanceField): string | null {
    return serverError(this.form, field);
  }

  hasClientError(field: MaintenanceField): boolean {
    const control = this.form.controls[field];
    return control.touched && control.invalid;
  }

  async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.formError.set(null);
    const input = this.toInput();
    try {
      const current = this.record();
      const result = current
        ? await this.facade.update(this.vehicleId(), current.id!, input)
        : await this.facade.create(this.vehicleId(), input);
      this.saved.emit(result);
      this.visible.set(false);
    } catch (error) {
      this.formError.set(applyProblemDetails(this.form, error));
    } finally {
      this.submitting.set(false);
    }
  }

  cancel(): void {
    this.visible.set(false);
  }

  /** Optional text fields go to the API as null when blank, matching the record shape. */
  private toInput(): MaintenanceInput {
    const value = this.form.getRawValue();
    return {
      ...value,
      serviceProvider: value.serviceProvider.trim() || null,
      notes: value.notes.trim() || null,
    };
  }

  private reset(record: MaintenanceRecordDto | null): void {
    this.formError.set(null);
    this.form.reset({ datePerformed: todayIso() });
    if (record) {
      this.form.setValue({
        description: record.description ?? '',
        costUsd: record.costUsd ?? 0,
        datePerformed: record.datePerformed ?? todayIso(),
        mileageAtService: record.mileageAtService ?? 0,
        serviceProvider: record.serviceProvider ?? '',
        notes: record.notes ?? '',
      });
    }
  }
}
