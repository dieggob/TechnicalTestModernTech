import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, input, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { VehicleDto } from '../../../api/models/vehicle-dto';
import { MaintenanceFacade } from '../../maintenance/maintenance.facade';
import { VehiclesFacade } from '../vehicles.facade';

/** One vehicle's details and its maintenance history, newest first. */
@Component({
  selector: 'app-vehicle-detail',
  imports: [CurrencyPipe, DatePipe, RouterLink, ButtonModule, MessageModule, TableModule],
  templateUrl: './vehicle-detail.component.html',
})
export class VehicleDetailComponent implements OnInit {
  /** Bound from the route parameter by the router (withComponentInputBinding). */
  readonly vehicleId = input.required<string>();

  protected readonly maintenance = inject(MaintenanceFacade);
  private readonly vehicles = inject(VehiclesFacade);

  readonly vehicle = signal<VehicleDto | null>(null);
  readonly vehicleError = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadVehicle(), this.maintenance.load(this.vehicleId())]);
  }

  private async loadVehicle(): Promise<void> {
    try {
      this.vehicle.set(await this.vehicles.get(this.vehicleId()));
    } catch {
      this.vehicleError.set('This vehicle could not be found.');
    }
  }
}
