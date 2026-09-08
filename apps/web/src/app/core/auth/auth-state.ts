import { computed, Injectable, signal } from '@angular/core';
import { StoredSession, TokenStorage } from './token-storage';

/**
 * Facade over the session: components and guards read signals, the login screen writes,
 * the interceptor reacts. Nothing else touches the token.
 */
@Injectable({ providedIn: 'root' })
export class AuthState {
  private readonly session = signal<StoredSession | null>(null);

  readonly token = computed(() => this.session()?.token ?? null);
  readonly userId = computed(() => this.session()?.userId ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly emailVerified = computed(() => this.session()?.emailVerified ?? false);

  constructor(private readonly storage: TokenStorage) {
    this.session.set(this.storage.read());
  }

  /** Called by the login screen with the API's AuthResult. */
  signIn(session: StoredSession): void {
    this.storage.write(session);
    this.session.set(session);
  }

  signOut(): void {
    this.storage.clear();
    this.session.set(null);
  }

  /** The verification page confirmed the address; the banner disappears without a new login. */
  markVerified(): void {
    const current = this.session();
    if (current && !current.emailVerified) {
      this.signIn({ ...current, emailVerified: true });
    }
  }
}
