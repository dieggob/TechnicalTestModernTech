import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthApi } from '../auth-api';
import { RegisterComponent } from './register.component';

describe('RegisterComponent', () => {
  let fixture: ComponentFixture<RegisterComponent>;
  let component: RegisterComponent;
  let register: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    register = vi.fn();
    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [provideRouter([]), { provide: AuthApi, useValue: { register } }],
    }).compileComponents();
    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('does not call the API while the form is invalid', async () => {
    component.form.setValue({ email: 'nope', password: 'short' });

    await component.submit();

    expect(register).not.toHaveBeenCalled();
    expect(component.form.controls.email.touched).toBe(true);
  });

  it('shows the success message after registering', async () => {
    register.mockResolvedValue({ message: 'Account created. Check your email to verify the address.' });
    component.form.setValue({ email: 'new@example.com', password: 'Secret123' });

    await component.submit();
    await fixture.whenStable();

    expect(register).toHaveBeenCalledWith('new@example.com', 'Secret123');
    expect(fixture.nativeElement.querySelector('[data-testid="register-success"]')?.textContent).toContain('Check your email');
  });

  it('binds server field errors to the controls and shows the form message for 409', async () => {
    register.mockRejectedValueOnce(
      new HttpErrorResponse({ status: 400, error: { errors: { password: ['Password must contain a digit.'] } } }),
    );
    component.form.setValue({ email: 'new@example.com', password: 'lettersonly' });
    await component.submit();
    expect(component.serverError('password')).toBe('Password must contain a digit.');
    expect(component.formError()).toBeNull();

    register.mockRejectedValueOnce(new HttpErrorResponse({ status: 409, error: { title: 'Email is already registered.' } }));
    component.form.setValue({ email: 'dup@example.com', password: 'Secret123' });
    await component.submit();
    expect(component.formError()).toBe('Email is already registered.');
  });
});
