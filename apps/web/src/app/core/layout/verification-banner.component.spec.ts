import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AuthState } from '../auth/auth-state';
import { AuthApi } from '../../features/auth/auth-api';
import { VerificationBannerComponent } from './verification-banner.component';

describe('VerificationBannerComponent', () => {
  let fixture: ComponentFixture<VerificationBannerComponent>;
  let resendVerification: ReturnType<typeof vi.fn>;

  const signIn = (emailVerified: boolean) =>
    TestBed.inject(AuthState).signIn({ token: 'jwt', userId: 'u', email: 'me@example.com', emailVerified, expiresAt: '' });

  beforeEach(async () => {
    sessionStorage.clear();
    resendVerification = vi.fn().mockResolvedValue({ message: 'ok' });
    await TestBed.configureTestingModule({
      imports: [VerificationBannerComponent],
      providers: [{ provide: AuthApi, useValue: { resendVerification } }],
    }).compileComponents();
    fixture = TestBed.createComponent(VerificationBannerComponent);
  });

  const banner = () => fixture.nativeElement.querySelector('[data-testid="verification-banner"]');

  it('is hidden for anonymous visitors and verified accounts', async () => {
    await fixture.whenStable();
    expect(banner()).toBeNull();

    signIn(true);
    await fixture.whenStable();
    expect(banner()).toBeNull();
  });

  it('is shown for an unverified account and resends with the session email', async () => {
    signIn(false);
    await fixture.whenStable();
    expect(banner()).not.toBeNull();

    await fixture.componentInstance.resend();
    await fixture.whenStable();

    expect(resendVerification).toHaveBeenCalledWith('me@example.com');
    expect(fixture.nativeElement.querySelector('[data-testid="resend-confirmation"]')).not.toBeNull();
  });
});
