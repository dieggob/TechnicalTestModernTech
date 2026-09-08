import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MessageModule } from 'primeng/message';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AuthState } from '../../../core/auth/auth-state';
import { AuthApi } from '../auth-api';

type VerifyStatus = 'verifying' | 'verified' | 'invalid';

/**
 * Target of the emailed verification link (/verify?token=...). Works for signed-in and
 * anonymous visitors alike; when a session exists, the banner disappears without a new login.
 */
@Component({
  selector: 'app-verify',
  imports: [RouterLink, MessageModule, ProgressSpinnerModule],
  template: `
    <section class="auth-card">
      <h1>Email verification</h1>
      @switch (status()) {
        @case ('verifying') {
          <p-progress-spinner styleClass="spinner" ariaLabel="Verifying" />
          <p>Verifying your email address…</p>
        }
        @case ('verified') {
          <p-message severity="success" data-testid="verify-success">Your email address is verified.</p-message>
          <p>
            @if (auth.isAuthenticated()) {
              <a routerLink="/vehicles">Go to your vehicles</a>.
            } @else {
              <a routerLink="/login">Log in</a> to continue.
            }
          </p>
        }
        @case ('invalid') {
          <p-message severity="error" data-testid="verify-invalid">This link is invalid or has expired.</p-message>
          <p>
            @if (auth.isAuthenticated()) {
              Use the banner above to request a new link.
            } @else {
              <a routerLink="/login">Log in</a> and request a new link from the banner.
            }
          </p>
        }
      }
    </section>
  `,
})
export class VerifyComponent implements OnInit {
  protected readonly auth = inject(AuthState);
  private readonly authApi = inject(AuthApi);
  private readonly route = inject(ActivatedRoute);

  readonly status = signal<VerifyStatus>('verifying');

  async ngOnInit(): Promise<void> {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.status.set('invalid');
      return;
    }

    try {
      await this.authApi.verifyEmail(token);
      this.auth.markVerified();
      this.status.set('verified');
    } catch {
      this.status.set('invalid');
    }
  }
}
