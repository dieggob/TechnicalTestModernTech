import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { AuthState } from '../../../core/auth/auth-state';
import { applyProblemDetails, serverError } from '../../../shared/problem-details';
import { AuthApi } from '../auth-api';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, ButtonModule, InputTextModule, MessageModule],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly authApi = inject(AuthApi);
  private readonly auth = inject(AuthState);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);

  readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  readonly submitting = signal(false);
  readonly formError = signal<string | null>(null);

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
      const result = await this.authApi.login(email, password);
      this.auth.signIn({
        token: result.token!,
        userId: result.userId!,
        email,
        emailVerified: result.emailVerified ?? false,
        expiresAt: result.expiresAt!,
      });
      await this.router.navigateByUrl(this.route.snapshot.queryParamMap.get('returnUrl') || '/vehicles');
    } catch (error) {
      this.formError.set(applyProblemDetails(this.form, error));
    } finally {
      this.submitting.set(false);
    }
  }
}
