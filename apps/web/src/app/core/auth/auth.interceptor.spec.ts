import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthState } from './auth-state';
import { authInterceptor, EMAIL_NOT_VERIFIED_CODE } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: AuthState;
  let navigate: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    sessionStorage.clear();
    navigate = vi.fn().mockResolvedValue(true);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigate } },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthState);
  });

  afterEach(() => backend.verify());

  it('adds the bearer header when a session exists', () => {
    auth.signIn({ token: 'jwt', userId: 'u', email: 'u@example.com', emailVerified: true, expiresAt: '' });

    http.get('/api/v1/vehicles').subscribe();

    const request = backend.expectOne('/api/v1/vehicles');
    expect(request.request.headers.get('Authorization')).toBe('Bearer jwt');
    request.flush([]);
  });

  it('sends no header without a session', () => {
    http.get('/api/v1/auth/login').subscribe();

    const request = backend.expectOne('/api/v1/auth/login');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('ends the session and goes to /login on 401', () => {
    auth.signIn({ token: 'jwt', userId: 'u', email: 'u@example.com', emailVerified: true, expiresAt: '' });

    http.get('/api/v1/vehicles').subscribe({ error: () => undefined });
    backend.expectOne('/api/v1/vehicles').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(auth.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });

  it('goes to /verify on 403 EmailNotVerified and keeps the session', () => {
    auth.signIn({ token: 'jwt', userId: 'u', email: 'u@example.com', emailVerified: false, expiresAt: '' });

    http.get('/api/v1/vehicles').subscribe({ error: () => undefined });
    backend
      .expectOne('/api/v1/vehicles')
      .flush({ code: EMAIL_NOT_VERIFIED_CODE }, { status: 403, statusText: 'Forbidden' });

    expect(auth.isAuthenticated()).toBe(true);
    expect(navigate).toHaveBeenCalledWith(['/verify']);
  });
});
