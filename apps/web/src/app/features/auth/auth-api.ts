import { inject, Injectable } from '@angular/core';
import { Api } from '../../api/api';
import { authForgotPassword } from '../../api/fn/auth/auth-forgot-password';
import { authLogin } from '../../api/fn/auth/auth-login';
import { authRegister } from '../../api/fn/auth/auth-register';
import { authResendVerification } from '../../api/fn/auth/auth-resend-verification';
import { authResetPassword } from '../../api/fn/auth/auth-reset-password';
import { authVerifyEmail } from '../../api/fn/auth/auth-verify-email';
import { AuthResult } from '../../api/models/auth-result';
import { MessageResponse } from '../../api/models/message-response';

/**
 * Facade over the generated auth operations: screens call one method per behaviour and never
 * touch the generated functions directly.
 */
@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly api = inject(Api);

  register(email: string, password: string): Promise<MessageResponse> {
    return this.api.invoke(authRegister, { body: { email, password } });
  }

  login(email: string, password: string): Promise<AuthResult> {
    return this.api.invoke(authLogin, { body: { email, password } });
  }

  verifyEmail(token: string): Promise<MessageResponse> {
    return this.api.invoke(authVerifyEmail, { body: { token } });
  }

  resendVerification(email: string): Promise<MessageResponse> {
    return this.api.invoke(authResendVerification, { body: { email } });
  }

  forgotPassword(email: string): Promise<MessageResponse> {
    return this.api.invoke(authForgotPassword, { body: { email } });
  }

  resetPassword(token: string, newPassword: string): Promise<MessageResponse> {
    return this.api.invoke(authResetPassword, { body: { token, newPassword } });
  }
}
