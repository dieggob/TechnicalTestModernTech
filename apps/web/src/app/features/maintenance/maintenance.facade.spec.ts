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
});
