import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { applyProblemDetails, serverError } from '../../../shared/problem-details';
import { AuthApi } from '../auth-api';

/** Target of the emailed reset link (/reset-password?token=...). Sets a new password; does not log in. */
@Component({
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, RouterLink, ButtonModule, InputTextModule, MessageModule],
  template: `
    <section class="auth-card">
      <h1>Choose a new password</h1>
      @if (!token) {
        <p-message severity="error" data-testid="reset-invalid">This link is invalid or has expired.</p-message>
        <p><a routerLink="/forgot-password">Request a new one</a>.</p>
      } @else if (done()) {
        <p-message severity="success" data-testid="reset-success">Password changed. You can log in with it now.</p-message>
        <p><a routerLink="/login">Log in</a></p>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label for="newPassword">New password</label>
            <input pInputText id="newPassword" type="password" formControlName="newPassword" autocomplete="new-password" />
            @if (form.controls.newPassword.touched && form.controls.newPassword.invalid) {
              <small class="error" data-testid="password-error">Password must be at least 8 characters.</small>
            } @else if (serverError(); as message) {
              <small class="error" data-testid="password-error">{{ message }}</small>
            }
          </div>
          @if (formError(); as message) {
            <p-message severity="error" data-testid="form-error">{{ message }}</p-message>
          }
          <p-button type="submit" label="Change password" [loading]="submitting()" />
        </form>
      }
    </section>
  `,
})
export class ResetPasswordComponent {
  private readonly authApi = inject(AuthApi);
  private readonly formBuilder = inject(FormBuilder);

  readonly token = inject(ActivatedRoute).snapshot.queryParamMap.get('token');
  readonly form = this.formBuilder.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
  });
  readonly submitting = signal(false);
  readonly done = signal(false);
  readonly formError = signal<string | null>(null);

  serverError(): string | null {
    return serverError(this.form, 'newPassword');
  }

  async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (!this.token || this.form.invalid || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.formError.set(null);
    try {
      await this.authApi.resetPassword(this.token, this.form.getRawValue().newPassword);
      this.done.set(true);
    } catch (error) {
      // A rejected token arrives as a field error on "token", which has no control: surface it on the form.
      this.formError.set(applyProblemDetails(this.form, error) ?? 'This link is invalid or has expired.');
    } finally {
      this.submitting.set(false);
    }
  }
}
