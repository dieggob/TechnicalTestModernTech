import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthState } from '../../../core/auth/auth-state';
import { AuthApi } from '../auth-api';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let login: ReturnType<typeof vi.fn>;
  let navigateByUrl: ReturnType<typeof vi.spyOn>;

  beforeEach(async () => {
    sessionStorage.clear();
    login = vi.fn();
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideRouter([]), { provide: AuthApi, useValue: { login } }],
    }).compileComponents();
    navigateByUrl = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('signs in and goes to /vehicles on success', async () => {
    login.mockResolvedValue({ token: 'jwt', userId: 'u1', emailVerified: false, expiresAt: '2026-09-09T00:00:00Z' });
    component.form.setValue({ email: 'me@example.com', password: 'Secret123' });

    await component.submit();

    const auth = TestBed.inject(AuthState);
    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.email()).toBe('me@example.com');
    expect(auth.emailVerified()).toBe(false);
    expect(navigateByUrl).toHaveBeenCalledWith('/vehicles');
  });

  it('shows the API message on 401 and stays anonymous', async () => {
    login.mockRejectedValue(new HttpErrorResponse({ status: 401, error: { title: 'Invalid email or password.' } }));
    component.form.setValue({ email: 'me@example.com', password: 'Wrong999' });

    await component.submit();

    expect(component.formError()).toBe('Invalid email or password.');
    expect(TestBed.inject(AuthState).isAuthenticated()).toBe(false);
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('does not call the API while the form is invalid', async () => {
    component.form.setValue({ email: '', password: '' });

    await component.submit();

    expect(login).not.toHaveBeenCalled();
  });
});
