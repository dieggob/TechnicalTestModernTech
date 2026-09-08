import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthApi } from '../auth-api';
import { ForgotPasswordComponent } from './forgot-password.component';

describe('ForgotPasswordComponent', () => {
  let fixture: ComponentFixture<ForgotPasswordComponent>;
  let forgotPassword: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    forgotPassword = vi.fn().mockResolvedValue({ message: 'ok' });
    await TestBed.configureTestingModule({
      imports: [ForgotPasswordComponent],
      providers: [provideRouter([]), { provide: AuthApi, useValue: { forgotPassword } }],
    }).compileComponents();
    fixture = TestBed.createComponent(ForgotPasswordComponent);
    await fixture.whenStable();
  });

  it('shows the same confirmation after submitting any valid email', async () => {
    fixture.componentInstance.form.setValue({ email: 'anyone@example.com' });

    await fixture.componentInstance.submit();
    await fixture.whenStable();

    expect(forgotPassword).toHaveBeenCalledWith('anyone@example.com');
    expect(fixture.nativeElement.querySelector('[data-testid="forgot-success"]')).not.toBeNull();
  });

  it('does not call the API for an invalid email', async () => {
    fixture.componentInstance.form.setValue({ email: 'nope' });

    await fixture.componentInstance.submit();

    expect(forgotPassword).not.toHaveBeenCalled();
  });
});
