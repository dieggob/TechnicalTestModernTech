import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthState } from './auth-state';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  const run = (url: string) =>
    TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    );

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({});
  });

  it('lets an authenticated user through', () => {
    TestBed.inject(AuthState).signIn({ token: 'jwt', userId: 'u', email: 'u@example.com', emailVerified: false, expiresAt: '' });

    expect(run('/vehicles')).toBe(true);
  });

  it('redirects an anonymous visitor to /login with the return url', () => {
    const result = run('/vehicles/42') as UrlTree;

    expect(result instanceof UrlTree).toBe(true);
    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/login?returnUrl=%2Fvehicles%2F42');
  });
});
