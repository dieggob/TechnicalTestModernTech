import { Component, inject, OnInit, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { VehicleDto } from '../../../api/models/vehicle-dto';
import { VehicleFormComponent } from '../vehicle-form/vehicle-form.component';
import { VehiclesFacade } from '../vehicles.facade';

/** The signed-in user's vehicles, with add and edit through one dialog. */
@Component({
  selector: 'app-vehicle-list',
  imports: [ButtonModule, MessageModule, TableModule, VehicleFormComponent],
  templateUrl: './vehicle-list.component.html',
})
export class VehicleListComponent implements OnInit {
  protected readonly facade = inject(VehiclesFacade);

  readonly formVisible = signal(false);
  readonly editing = signal<VehicleDto | null>(null);

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
}
