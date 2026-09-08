import { inject, Injectable, signal } from '@angular/core';
import { Api } from '../../api/api';
import { vehicleCreate } from '../../api/fn/vehicle/vehicle-create';
import { vehicleDelete } from '../../api/fn/vehicle/vehicle-delete';
import { vehicleGet } from '../../api/fn/vehicle/vehicle-get';
import { vehicleList } from '../../api/fn/vehicle/vehicle-list';
import { vehicleUpdate } from '../../api/fn/vehicle/vehicle-update';
import { VehicleDto } from '../../api/models/vehicle-dto';
import { VehicleInput } from '../../api/models/vehicle-input';

/**
 * Facade for the vehicles feature: holds the list as signals and is the only caller of the
 * generated vehicle operations. Components stay presentational.
 */
@Injectable({ providedIn: 'root' })
export class VehiclesFacade {
  private readonly api = inject(Api);

  readonly vehicles = signal<VehicleDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.vehicles.set(await this.api.invoke(vehicleList));
    } catch {
      this.error.set('Your vehicles could not be loaded.');
    } finally {
      this.loading.set(false);
    }
  }

  get(vehicleId: string): Promise<VehicleDto> {
    return this.api.invoke(vehicleGet, { vehicleId });
  }

  async create(input: VehicleInput): Promise<VehicleDto> {
    const created = await this.api.invoke(vehicleCreate, { body: input });
    this.vehicles.update((list) => [...list, created]);
    return created;
  }

  async update(vehicleId: string, input: VehicleInput): Promise<VehicleDto> {
    const updated = await this.api.invoke(vehicleUpdate, { vehicleId, body: input });
    this.vehicles.update((list) => list.map((vehicle) => (vehicle.id === vehicleId ? updated : vehicle)));
    return updated;
  }

  async delete(vehicleId: string): Promise<void> {
    await this.api.invoke(vehicleDelete, { vehicleId });
    this.vehicles.update((list) => list.filter((vehicle) => vehicle.id !== vehicleId));
  }
}
