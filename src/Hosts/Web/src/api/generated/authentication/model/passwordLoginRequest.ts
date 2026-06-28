export interface PasswordLoginRequest {
  userNameOrEmail: string;
  password: string;
  returnUrl?: string | null;
  rememberMe?: boolean;
}
