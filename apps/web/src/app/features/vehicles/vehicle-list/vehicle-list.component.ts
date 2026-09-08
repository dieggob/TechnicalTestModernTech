import { Component, inject, OnInit, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { VehicleDto } from '../../../api/models/vehicle-dto';
import { VehicleFormComponent } from '../vehicle-form/vehicle-form.component';
import { VehiclesFacade } from '../vehicles.facade';

/**
 * The signed-in user's vehicles, with add and edit through one dialog and delete behind a
 * confirmation. The confirmation is a signal-driven dialog: the app runs zoneless, so state
 * that changes outside a template event must be a signal to render.
 */
@Component({
  selector: 'app-vehicle-list',
  imports: [ButtonModule, DialogModule, MessageModule, TableModule, VehicleFormComponent],
  templateUrl: './vehicle-list.component.html',
})
export class VehicleListComponent implements OnInit {
  protected readonly facade = inject(VehiclesFacade);

  readonly formVisible = signal(false);
  readonly editing = signal<VehicleDto | null>(null);
  readonly deleting = signal<VehicleDto | null>(null);
  readonly deletingInProgress = signal(false);
  readonly actionError = signal<string | null>(null);

  ngOnInit(): void {
    void this.facade.load();
  }

  add(): void {
    this.editing.set(null);
    this.formVisible.set(true);
  }

  edit(vehicle: VehicleDto): void {
    this.editing.set(vehicle);
    this.formVisible.set(true);
  }

  /** Deleting removes the vehicle's maintenance records too (design decision), so it always asks first. */
  confirmDelete(vehicle: VehicleDto): void {
    this.actionError.set(null);
    this.deleting.set(vehicle);
  }

  keep(): void {
    this.deleting.set(null);
  }

  async deleteConfirmed(): Promise<void> {
    const vehicle = this.deleting();
    if (!vehicle || this.deletingInProgress()) {
      return;
    }
    this.deletingInProgress.set(true);
    try {
      await this.facade.delete(vehicle.id!);
      this.deleting.set(null);
    } catch {
      this.actionError.set('The vehicle could not be deleted. Please try again.');
      this.deleting.set(null);
    } finally {
      this.deletingInProgress.set(false);
    }
  }
}
