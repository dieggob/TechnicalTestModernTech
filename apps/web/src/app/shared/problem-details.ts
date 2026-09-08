import { HttpErrorResponse } from '@angular/common/http';
import { FormGroup } from '@angular/forms';

/** Shape of the API's ProblemDetails and ValidationProblemDetails bodies. */
export interface ProblemDetailsBody {
  title?: string;
  status?: number;
  code?: string;
  errors?: Record<string, string[]>;
}

/** Error key under which server-side field messages are stored on a control. */
export const SERVER_ERROR = 'server';

/**
 * The one way forms bind API errors: field errors from a 400 land on the matching controls
 * under the `server` key; anything else becomes a single message for the form.
 * Returns the form-level message, or null when every error was bound to a field.
 */
export function applyProblemDetails(form: FormGroup, error: unknown): string | null {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Something went wrong. Please try again.';
  }

  const body = (error.error ?? {}) as ProblemDetailsBody;
  let unbound = false;
  for (const [field, messages] of Object.entries(body.errors ?? {})) {
    const control = form.get(field);
    if (control) {
      control.setErrors({ ...(control.errors ?? {}), [SERVER_ERROR]: messages.join(' ') });
      control.markAsTouched();
    } else {
      unbound = true;
    }
  }

  if (body.errors && !unbound) {
    return null;
  }
  return body.title ?? (error.status === 0 ? 'The server cannot be reached.' : 'Something went wrong. Please try again.');
}

/** The server message for a control, if any. */
export function serverError(form: FormGroup, field: string): string | null {
  const control = form.get(field);
  return control?.touched && control.errors?.[SERVER_ERROR] ? (control.errors[SERVER_ERROR] as string) : null;
}
