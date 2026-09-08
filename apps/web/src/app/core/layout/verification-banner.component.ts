import { Component, inject, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { AuthApi } from '../../features/auth/auth-api';
import { AuthState } from '../auth/auth-state';

/**
 * Shown while the signed-in account is unverified. Verification does not gate features by
 * default; this is the nudge the design calls for, with a resend action.
 */
@Component({
  selector: 'app-verification-banner',
  imports: [ButtonModule, MessageModule],
  template: `
    @if (auth.isAuthenticated() && !auth.emailVerified()) {
      <p-message severity="warn" styleClass="verification-banner" data-testid="verification-banner">
        <span>Your email address is not verified yet. Check your inbox for the link.</span>
        @if (sent()) {
          <span data-testid="resend-confirmation">A new link is on its way.</span>
        } @else {
          <p-button label="Resend verification email" [text]="true" size="small" [loading]="sending()" (onClick)="resend()" />
        }
      </p-message>
    }
  `,
})
export class VerificationBannerComponent {
  protected readonly auth = inject(AuthState);
  private readonly authApi = inject(AuthApi);

  readonly sending = signal(false);
  readonly sent = signal(false);

  async resend(): Promise<void> {
    const email = this.auth.email();
    if (!email || this.sending()) {
      return;
    }
    this.sending.set(true);
    try {
      await this.authApi.resendVerification(email);
      this.sent.set(true);
    } finally {
      this.sending.set(false);
    }
  }
}
