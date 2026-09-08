import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { Toolbar } from 'primeng/toolbar';
import { AuthState } from './core/auth/auth-state';
import { VerificationBannerComponent } from './core/layout/verification-banner.component';

/**
 * Application shell: toolbar with the title and session actions, the verification banner,
 * and the router outlet.
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, Toolbar, ButtonModule, VerificationBannerComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly title = 'Vehicle Maintenance Tracker';
  protected readonly auth = inject(AuthState);
  private readonly router = inject(Router);

  async logOut(): Promise<void> {
    this.auth.signOut();
    await this.router.navigateByUrl('/login');
  }
}
