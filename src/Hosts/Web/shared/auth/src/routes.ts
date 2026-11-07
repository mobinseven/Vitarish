/** Routes shared by the authentication surfaces in every Web app. */
export const AuthenticationWebRoutes = {
  SignIn: "/login",
  PasswordRecovery: "/login/forgot-password",
  Continue: "/continue",
  Handoff: "/handoff",
  Account: "/account",
  AccountSecurity: "/account/security",
} as const;

export const AuthenticationSearchParams = {
  ReturnUrl: "returnUrl",
  Request: "request",
} as const;
