import { TestBed } from '@angular/core/testing';
import { AuthState } from './auth-state';
import { StoredSession, TokenStorage } from './token-storage';

const session: StoredSession = {
  token: 'jwt',
  userId: 'user-1',
  emailVerified: false,
  expiresAt: '2026-09-09T00:00:00Z',
};

describe('AuthState', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({});
  });

  it('starts anonymous and becomes authenticated on signIn', () => {
    const auth = TestBed.inject(AuthState);

    expect(auth.isAuthenticated()).toBe(false);
    auth.signIn(session);
    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.token()).toBe('jwt');
    expect(auth.emailVerified()).toBe(false);
  });

  it('restores a stored session on creation', () => {
    TestBed.inject(TokenStorage).write(session);

    const auth = TestBed.inject(AuthState);

    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.userId()).toBe('user-1');
  });

  it('signOut clears storage and state', () => {
    const auth = TestBed.inject(AuthState);
    auth.signIn(session);

    auth.signOut();

    expect(auth.isAuthenticated()).toBe(false);
    expect(TestBed.inject(TokenStorage).read()).toBeNull();
  });

  it('markVerified flips the flag without a new login', () => {
    const auth = TestBed.inject(AuthState);
    auth.signIn(session);

    auth.markVerified();

    expect(auth.emailVerified()).toBe(true);
    expect(auth.token()).toBe('jwt');
  });
});
