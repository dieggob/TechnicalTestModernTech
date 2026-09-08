import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { AuthApi } from '../auth-api';
import { ResetPasswordComponent } from './reset-password.component';

describe('ResetPasswordComponent', () => {
  let resetPassword: ReturnType<typeof vi.fn>;

  const setup = async (token: string | null) => {
    await TestBed.configureTestingModule({
      imports: [ResetPasswordComponent],
      providers: [
        provideRouter([]),
        { provide: AuthApi, useValue: { resetPassword } },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(token ? { token } : {}) } } },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(ResetPasswordComponent);
    await fixture.whenStable();
    return fixture;
  };

  beforeEach(() => {
    resetPassword = vi.fn();
  });

  it('changes the password and shows the success state', async () => {
    resetPassword.mockResolvedValue({ message: 'ok' });
    const fixture = await setup('tok');
    fixture.componentInstance.form.setValue({ newPassword: 'NewSecret2' });

    await fixture.componentInstance.submit();
    await fixture.whenStable();

    expect(resetPassword).toHaveBeenCalledWith('tok', 'NewSecret2');
    expect(fixture.nativeElement.querySelector('[data-testid="reset-success"]')).not.toBeNull();
  });

  it('surfaces a rejected token as a form error', async () => {
    resetPassword.mockRejectedValue(
      new HttpErrorResponse({ status: 400, error: { errors: { token: ['The link is invalid or has expired.'] } } }),
    );
    const fixture = await setup('expired');
    fixture.componentInstance.form.setValue({ newPassword: 'NewSecret2' });

    await fixture.componentInstance.submit();

    expect(fixture.componentInstance.formError()).toContain('invalid or has expired');
  });

  it('shows the invalid state without a token', async () => {
    const fixture = await setup(null);

    expect(fixture.nativeElement.querySelector('[data-testid="reset-invalid"]')).not.toBeNull();
  });
});
