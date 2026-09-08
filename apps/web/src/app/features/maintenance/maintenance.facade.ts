import { inject, Injectable, signal } from '@angular/core';
import { Api } from '../../api/api';
import { maintenanceCreate } from '../../api/fn/maintenance/maintenance-create';
import { maintenanceList } from '../../api/fn/maintenance/maintenance-list';
import { maintenanceUpdate } from '../../api/fn/maintenance/maintenance-update';
import { MaintenanceInput } from '../../api/models/maintenance-input';
import { MaintenanceRecordDto } from '../../api/models/maintenance-record-dto';
import { MaintenanceWriteResult } from '../../api/models/maintenance-write-result';

/**
 * Facade for one vehicle's maintenance history: holds the records as signals, newest first,
 * and is the only caller of the generated maintenance operations. Writes return the API's
 * result so the host can apply the vehicle's advanced mileage.
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

  async create(vehicleId: string, input: MaintenanceInput): Promise<MaintenanceWriteResult> {
    const result = await this.api.invoke(maintenanceCreate, { vehicleId, body: input });
    this.records.update((list) => sortNewestFirst([...list, result.record!]));
    return result;
  }

  async update(vehicleId: string, recordId: string, input: MaintenanceInput): Promise<MaintenanceWriteResult> {
    const result = await this.api.invoke(maintenanceUpdate, { vehicleId, recordId, body: input });
    this.records.update((list) => sortNewestFirst(list.map((record) => (record.id === recordId ? result.record! : record))));
    return result;
  }
}

/** The API's order: date performed descending, then most recently created first. */
function sortNewestFirst(records: MaintenanceRecordDto[]): MaintenanceRecordDto[] {
  return [...records].sort(
    (a, b) => (b.datePerformed ?? '').localeCompare(a.datePerformed ?? '') || (b.createdAt ?? '').localeCompare(a.createdAt ?? ''),
  );
}
