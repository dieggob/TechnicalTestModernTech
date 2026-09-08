import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, input, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { MaintenanceRecordDto } from '../../../api/models/maintenance-record-dto';
import { MaintenanceWriteResult } from '../../../api/models/maintenance-write-result';
import { VehicleDto } from '../../../api/models/vehicle-dto';
import { MaintenanceFormComponent } from '../../maintenance/maintenance-form/maintenance-form.component';
import { MaintenanceFacade } from '../../maintenance/maintenance.facade';
import { VehiclesFacade } from '../vehicles.facade';

/**
 * One vehicle's details and its maintenance history, newest first, with the log/edit dialog and a
 * signal-driven delete confirmation (the app is zoneless, so dialog state lives in signals).
 */
@Component({
  selector: 'app-vehicle-detail',
  imports: [CurrencyPipe, DatePipe, RouterLink, ButtonModule, DialogModule, MessageModule, TableModule, MaintenanceFormComponent],
  templateUrl: './vehicle-detail.component.html',
})
export class VehicleDetailComponent implements OnInit {
  /** Bound from the route parameter by the router (withComponentInputBinding). */
  readonly vehicleId = input.required<string>();

  protected readonly maintenance = inject(MaintenanceFacade);
  private readonly vehicles = inject(VehiclesFacade);

  readonly vehicle = signal<VehicleDto | null>(null);
  readonly vehicleError = signal<string | null>(null);
  readonly formVisible = signal(false);
  readonly editing = signal<MaintenanceRecordDto | null>(null);
  readonly deleting = signal<MaintenanceRecordDto | null>(null);
  readonly deletingInProgress = signal(false);
  readonly actionError = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadVehicle(), this.maintenance.load(this.vehicleId())]);
  }

  add(): void {
    this.editing.set(null);
    this.formVisible.set(true);
  }

  edit(record: MaintenanceRecordDto): void {
    this.editing.set(record);
    this.formVisible.set(true);
  }

  confirmDelete(record: MaintenanceRecordDto): void {
    this.actionError.set(null);
    this.deleting.set(record);
  }

  keep(): void {
    this.deleting.set(null);
  }

  /** Deleting a record never lowers the vehicle's mileage (design decision), so nothing else changes. */
  async deleteConfirmed(): Promise<void> {
    const record = this.deleting();
    if (!record || this.deletingInProgress()) {
      return;
    }
    this.deletingInProgress.set(true);
    try {
      await this.maintenance.delete(this.vehicleId(), record.id!);
    } catch {
      this.actionError.set('The record could not be deleted. Please try again.');
    } finally {
      this.deleting.set(null);
      this.deletingInProgress.set(false);
    }
  }

  /** A record was written: the vehicle's mileage may have advanced with it. */
  onSaved(result: MaintenanceWriteResult): void {
    this.vehicle.update((current) =>
      current && result.vehicleCurrentMileage !== undefined ? { ...current, currentMileage: result.vehicleCurrentMileage } : current,
    );
  }

  private async loadVehicle(): Promise<void> {
    try {
      this.vehicle.set(await this.vehicles.get(this.vehicleId()));
    } catch {
      this.vehicleError.set('This vehicle could not be found.');
    }
  }
}
