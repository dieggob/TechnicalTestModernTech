import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { applyProblemDetails, serverError } from '../../../shared/problem-details';
import { AuthApi } from '../auth-api';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, ButtonModule, InputTextModule, PasswordModule, MessageModule],
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  private readonly authApi = inject(AuthApi);
  private readonly formBuilder = inject(FormBuilder);

  readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  readonly submitting = signal(false);
  readonly formError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  serverError(field: 'email' | 'password'): string | null {
    return serverError(this.form, field);
  }

  async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.formError.set(null);
    const { email, password } = this.form.getRawValue();
    try {
      const response = await this.authApi.register(email, password);
      this.successMessage.set(response.message ?? 'Account created. Check your email to verify the address.');
    } catch (error) {
      this.formError.set(applyProblemDetails(this.form, error));
    } finally {
      this.submitting.set(false);
    }
  }
}
