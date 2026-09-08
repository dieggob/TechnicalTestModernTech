import { TestBed } from '@angular/core/testing';
import { Api } from '../../api/api';
import { VehiclesFacade } from './vehicles.facade';

describe('VehiclesFacade', () => {
  let invoke: ReturnType<typeof vi.fn>;
  let facade: VehiclesFacade;

  beforeEach(() => {
    invoke = vi.fn();
    TestBed.configureTestingModule({ providers: [{ provide: Api, useValue: { invoke } }] });
    facade = TestBed.inject(VehiclesFacade);
  });

  it('load fills the list and clears the error', async () => {
    invoke.mockResolvedValue([{ id: '1', vin: 'A' }]);

    await facade.load();

    expect(facade.vehicles()).toEqual([{ id: '1', vin: 'A' }]);
    expect(facade.loading()).toBe(false);
    expect(facade.error()).toBeNull();
  });

  it('load reports a failure without throwing', async () => {
    invoke.mockRejectedValue(new Error('boom'));

    await facade.load();

    expect(facade.error()).toContain('could not be loaded');
    expect(facade.vehicles()).toEqual([]);
  });

  it('create appends, update replaces, delete removes', async () => {
    invoke.mockResolvedValueOnce({ id: '1', vin: 'A' });
    await facade.create({ make: 'T', model: 'C', year: 2020, vin: 'A', licensePlate: 'P', currentMileage: 1 });
    expect(facade.vehicles().map((v) => v.id)).toEqual(['1']);

    invoke.mockResolvedValueOnce({ id: '1', vin: 'B' });
    await facade.update('1', { make: 'T', model: 'C', year: 2020, vin: 'B', licensePlate: 'P', currentMileage: 1 });
    expect(facade.vehicles()[0].vin).toBe('B');

    invoke.mockResolvedValueOnce(undefined);
    await facade.delete('1');
    expect(facade.vehicles()).toEqual([]);
  });
});
