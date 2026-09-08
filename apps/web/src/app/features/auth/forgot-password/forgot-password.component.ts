import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { applyProblemDetails, serverError } from '../../../shared/problem-details';
import { AuthApi } from '../auth-api';

/** Asks for a reset link. The API answers the same way for any address, and so does this screen. */
@Component({
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, RouterLink, ButtonModule, InputTextModule, MessageModule],
  template: `
    <section class="auth-card">
      <h1>Reset your password</h1>
      @if (sent()) {
        <p-message severity="success" data-testid="forgot-success">
          If that address is registered, a reset link is on its way. It stays valid for 30 minutes.
        </p-message>
        <p><a routerLink="/login">Back to log in</a></p>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label for="email">Email</label>
            <input pInputText id="email" type="email" formControlName="email" autocomplete="email" />
            @if (form.controls.email.touched && form.controls.email.invalid) {
              <small class="error" data-testid="email-error">A valid email is required.</small>
            } @else if (serverError(); as message) {
              <small class="error" data-testid="email-error">{{ message }}</small>
            }
          </div>
          @if (formError(); as message) {
            <p-message severity="error" data-testid="form-error">{{ message }}</p-message>
          }
          <p-button type="submit" label="Send reset link" [loading]="submitting()" />
        </form>
        <p><a routerLink="/login">Back to log in</a></p>
      }
    </section>
  `,
})
export class ForgotPasswordComponent {
  private readonly authApi = inject(AuthApi);
  private readonly formBuilder = inject(FormBuilder);

  readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });
  readonly submitting = signal(false);
  readonly sent = signal(false);
  readonly formError = signal<string | null>(null);

  serverError(): string | null {
    return serverError(this.form, 'email');
  }

  async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.formError.set(null);
    try {
      await this.authApi.forgotPassword(this.form.getRawValue().email);
      this.sent.set(true);
    } catch (error) {
      this.formError.set(applyProblemDetails(this.form, error));
    } finally {
      this.submitting.set(false);
    }
  }
}
