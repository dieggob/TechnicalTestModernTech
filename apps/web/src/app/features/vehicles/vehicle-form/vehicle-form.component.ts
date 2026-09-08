import { Component, effect, inject, input, model, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { VehicleDto } from '../../../api/models/vehicle-dto';
import { VehicleInput } from '../../../api/models/vehicle-input';
import { applyProblemDetails, serverError } from '../../../shared/problem-details';
import { VehiclesFacade } from '../vehicles.facade';

type VehicleField = keyof VehicleInput;

/**
 * One dialog for both registering and editing a vehicle: the mode follows whether a vehicle is
 * passed in. Server field errors land on the matching controls through the shared mapper.
 */
@Component({
  selector: 'app-vehicle-form',
  imports: [ReactiveFormsModule, ButtonModule, DialogModule, InputTextModule, MessageModule],
  templateUrl: './vehicle-form.component.html',
})
export class VehicleFormComponent {
  private readonly facade = inject(VehiclesFacade);
  private readonly formBuilder = inject(FormBuilder);

  /** Two-way bound by the host: opening resets the form to the vehicle being edited, or blank. */
  readonly visible = model(false);
  readonly vehicle = input<VehicleDto | null>(null);
  readonly saved = output<VehicleDto>();

  readonly form = this.formBuilder.nonNullable.group({
    make: ['', [Validators.required, Validators.maxLength(100)]],
    model: ['', [Validators.required, Validators.maxLength(100)]],
    year: [new Date().getFullYear(), [Validators.required, Validators.min(1886), Validators.max(new Date().getFullYear() + 1)]],
    vin: ['', [Validators.required, Validators.maxLength(17), Validators.pattern(/^[A-Za-z0-9]+$/)]],
    licensePlate: ['', [Validators.required, Validators.maxLength(20)]],
    currentMileage: [0, [Validators.required, Validators.min(0)]],
  });

  readonly submitting = signal(false);
  readonly formError = signal<string | null>(null);

  constructor() {
    effect(() => {
      if (this.visible()) {
        this.reset(this.vehicle());
      }
    });
  }

  get editing(): boolean {
    return this.vehicle() !== null;
  }

  serverError(field: VehicleField): string | null {
    return serverError(this.form, field);
  }

  hasClientError(field: VehicleField): boolean {
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
    const input: VehicleInput = this.form.getRawValue();
    try {
      const current = this.vehicle();
      const result = current ? await this.facade.update(current.id!, input) : await this.facade.create(input);
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

  private reset(vehicle: VehicleDto | null): void {
    this.formError.set(null);
    this.form.reset();
    if (vehicle) {
      this.form.setValue({
        make: vehicle.make ?? '',
        model: vehicle.model ?? '',
        year: vehicle.year ?? new Date().getFullYear(),
        vin: vehicle.vin ?? '',
        licensePlate: vehicle.licensePlate ?? '',
        currentMileage: vehicle.currentMileage ?? 0,
      });
    }
  }
}
