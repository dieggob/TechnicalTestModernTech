import { Component, inject, OnInit } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { VehiclesFacade } from '../vehicles.facade';

/** The signed-in user's vehicles. Add, edit, and delete actions arrive in the next slices. */
@Component({
  selector: 'app-vehicle-list',
  imports: [ButtonModule, MessageModule, TableModule],
  templateUrl: './vehicle-list.component.html',
})
export class VehicleListComponent implements OnInit {
  protected readonly facade = inject(VehiclesFacade);

  ngOnInit(): void {
    void this.facade.load();
  }
}
