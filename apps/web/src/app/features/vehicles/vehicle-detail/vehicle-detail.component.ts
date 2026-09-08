import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, input, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { MaintenanceRecordDto } from '../../../api/models/maintenance-record-dto';
import { MaintenanceWriteResult } from '../../../api/models/maintenance-write-result';
import { VehicleDto } from '../../../api/models/vehicle-dto';
import { MaintenanceFormComponent } from '../../maintenance/maintenance-form/maintenance-form.component';
import { MaintenanceFacade } from '../../maintenance/maintenance.facade';
import { VehiclesFacade } from '../vehicles.facade';

/** One vehicle's details and its maintenance history, newest first, with the log/edit dialog. */
@Component({
  selector: 'app-vehicle-detail',
  imports: [CurrencyPipe, DatePipe, RouterLink, ButtonModule, MessageModule, TableModule, MaintenanceFormComponent],
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
