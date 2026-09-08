import { inject, Injectable, signal } from '@angular/core';
import { Api } from '../../api/api';
import { maintenanceList } from '../../api/fn/maintenance/maintenance-list';
import { MaintenanceRecordDto } from '../../api/models/maintenance-record-dto';

/**
 * Facade for one vehicle's maintenance history: holds the records as signals and is the only
 * caller of the generated maintenance operations.
 */
@Injectable({ providedIn: 'root' })
export class MaintenanceFacade {
  private readonly api = inject(Api);

  readonly records = signal<MaintenanceRecordDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  async load(vehicleId: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.records.set(await this.api.invoke(maintenanceList, { vehicleId }));
    } catch {
      this.records.set([]);
      this.error.set('The maintenance history could not be loaded.');
    } finally {
      this.loading.set(false);
    }
  }
}
