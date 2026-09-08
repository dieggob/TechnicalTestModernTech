import { primeUiLicense } from './primeui-license';

describe('primeUiLicense', () => {
  it('is undefined when the build defines no key, so PrimeNG receives no license option', () => {
    expect(primeUiLicense).toBeUndefined();
  });
});
