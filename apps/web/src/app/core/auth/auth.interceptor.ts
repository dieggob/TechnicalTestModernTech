import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthState } from './auth-state';

/** ProblemDetails code the API sends when the verification flag gates an unverified account. */
export const EMAIL_NOT_VERIFIED_CODE = 'EmailNotVerified';

/**
 * Attaches the bearer token to every request and reacts to the two auth outcomes the API
 * defines: 401 ends the session; 403 with code EmailNotVerified sends the user to verify.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthState);
  const router = inject(Router);

  const token = auth.token();
  const outgoing = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(outgoing).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.status === 401 && auth.isAuthenticated()) {
          auth.signOut();
          void router.navigate(['/login']);
        } else if (error.status === 403 && error.error?.code === EMAIL_NOT_VERIFIED_CODE) {
          void router.navigate(['/verify']);
        }
      }
      return throwError(() => error);
    }),
  );
};
