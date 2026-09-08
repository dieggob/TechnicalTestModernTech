import { Injectable } from '@angular/core';

/**
 * The only place that touches browser storage for the session. sessionStorage, not
 * localStorage: the session ends with the tab, which limits the exposure of a stolen token.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorage {
  private static readonly key = 'vmt.session';

  read(): StoredSession | null {
    try {
      const raw = sessionStorage.getItem(TokenStorage.key);
      return raw ? (JSON.parse(raw) as StoredSession) : null;
    } catch {
      return null;
    }
  }

  write(session: StoredSession): void {
    try {
      sessionStorage.setItem(TokenStorage.key, JSON.stringify(session));
    } catch {
      // Storage unavailable (private mode, quota): the session lives in memory only.
    }
  }

  clear(): void {
    try {
      sessionStorage.removeItem(TokenStorage.key);
    } catch {
      // Nothing to clear.
    }
  }
}

export interface StoredSession {
  token: string;
  userId: string;
  email: string;
  emailVerified: boolean;
  expiresAt: string;
}
