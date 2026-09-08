/**
 * The PrimeUI license key for PrimeNG 22, injected at build time through Angular's `define`
 * option (`ng build --define "PRIMEUI_LICENSE='<key>'"`), which the repository scripts read from
 * the git-ignored `apps/web/.env` and the compose build from `docker/.env`. Without a key the
 * app still works but PrimeNG shows its license badge. The `typeof` guard keeps unit tests,
 * which do not define the identifier, working.
 */
declare const PRIMEUI_LICENSE: string | undefined;

export const primeUiLicense: string | undefined =
  typeof PRIMEUI_LICENSE === 'string' && PRIMEUI_LICENSE.trim() !== '' ? PRIMEUI_LICENSE.trim() : undefined;
