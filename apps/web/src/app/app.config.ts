import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';

import { environment } from '../environments/environment';
import { provideApiConfiguration } from './api/api-configuration';
import { authInterceptor } from './core/auth/auth.interceptor';
import { primeUiLicense } from './primeui-license';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideApiConfiguration(environment.apiRootUrl),
    providePrimeNG({ theme: { preset: Aura }, license: primeUiLicense }),
  ],
};
