import { TestBed } from '@angular/core/testing';
import { Api } from '../../api/api';
import { MaintenanceFacade } from './maintenance.facade';

describe('MaintenanceFacade', () => {
  let invoke: ReturnType<typeof vi.fn>;
  let facade: MaintenanceFacade;

  beforeEach(() => {
    invoke = vi.fn();
    TestBed.configureTestingModule({ providers: [{ provide: Api, useValue: { invoke } }] });
    facade = TestBed.inject(MaintenanceFacade);
  });

  it('load fills the records for the vehicle', async () => {
    invoke.mockResolvedValue([{ id: 'r1', description: 'Oil change' }]);

    await facade.load('v1');

    expect(invoke).toHaveBeenCalledWith(expect.any(Function), { vehicleId: 'v1' });
    expect(facade.records()).toEqual([{ id: 'r1', description: 'Oil change' }]);
    expect(facade.error()).toBeNull();
  });

  it('load reports a failure and empties the list', async () => {
    invoke.mockRejectedValue(new Error('boom'));

    await facade.load('v1');

    expect(facade.records()).toEqual([]);
    expect(facade.error()).toContain('could not be loaded');
  });

  it('create inserts the record in date order and returns the result', async () => {
    facade.records.set([{ id: 'r1', datePerformed: '2026-08-01' }]);
    const result = { record: { id: 'r2', datePerformed: '2026-09-01' }, vehicleCurrentMileage: 50000 };
    invoke.mockResolvedValue(result);
    const input = { description: 'Tyres', costUsd: 400, datePerformed: '2026-09-01', mileageAtService: 50000 };

    const returned = await facade.create('v1', input);

    expect(invoke).toHaveBeenCalledWith(expect.any(Function), { vehicleId: 'v1', body: input });
    expect(returned).toBe(result);
    expect(facade.records().map((record) => record.id)).toEqual(['r2', 'r1']);
  });

  it('update replaces the record and re-sorts', async () => {
    facade.records.set([
      { id: 'r2', datePerformed: '2026-09-01' },
      { id: 'r1', datePerformed: '2026-08-01' },
    ]);
    invoke.mockResolvedValue({ record: { id: 'r1', datePerformed: '2026-09-15', description: 'Moved' }, vehicleCurrentMileage: 1 });

    await facade.update('v1', 'r1', { description: 'Moved' });

    expect(invoke).toHaveBeenCalledWith(expect.any(Function), { vehicleId: 'v1', recordId: 'r1', body: { description: 'Moved' } });
    expect(facade.records().map((record) => record.id)).toEqual(['r1', 'r2']);
    expect(facade.records()[0].description).toBe('Moved');
  });
});
