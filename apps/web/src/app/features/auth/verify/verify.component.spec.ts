import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { AuthState } from '../../../core/auth/auth-state';
import { AuthApi } from '../auth-api';
import { VerifyComponent } from './verify.component';

describe('VerifyComponent', () => {
  let verifyEmail: ReturnType<typeof vi.fn>;

  const configure = (token: string | null) =>
    TestBed.configureTestingModule({
      imports: [VerifyComponent],
      providers: [
        provideRouter([]),
        { provide: AuthApi, useValue: { verifyEmail } },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(token ? { token } : {}) } } },
      ],
    }).compileComponents();

  const render = async () => {
    const fixture = TestBed.createComponent(VerifyComponent);
    await fixture.whenStable();
    return fixture;
  };

  const setup = async (token: string | null) => {
    await configure(token);
    return render();
  };

  beforeEach(() => {
    sessionStorage.clear();
    verifyEmail = vi.fn();
  });

  it('verifies the token and marks the current session verified', async () => {
    verifyEmail.mockResolvedValue({ message: 'ok' });
    await configure('abc');
    TestBed.inject(AuthState).signIn({ token: 'jwt', userId: 'u', email: 'me@example.com', emailVerified: false, expiresAt: '' });

    const fixture = await render();

    expect(verifyEmail).toHaveBeenCalledWith('abc');
    expect(fixture.nativeElement.querySelector('[data-testid="verify-success"]')).not.toBeNull();
    expect(TestBed.inject(AuthState).emailVerified()).toBe(true);
  });

  it('shows the invalid state when the API rejects the token', async () => {
    verifyEmail.mockRejectedValue(new HttpErrorResponse({ status: 400 }));

    const fixture = await setup('expired');

    expect(fixture.nativeElement.querySelector('[data-testid="verify-invalid"]')).not.toBeNull();
  });

  it('shows the invalid state without calling the API when the token is missing', async () => {
    const fixture = await setup(null);

    expect(verifyEmail).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('[data-testid="verify-invalid"]')).not.toBeNull();
  });
});
