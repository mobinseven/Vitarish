const signInPath = "/login";

/** Routes shared by the authentication surfaces in every Web app. */
export const AuthenticationWebRoutes = {
  SignIn: signInPath,
  SignInPassword: signInPath + "/password",
  SignInPhone: signInPath + "/phone",
  SignInEmail: signInPath + "/email",
  SignInPasskey: signInPath + "/passkey",
  PasswordRecovery: signInPath + "/forgot-password",
  Continue: "/continue",
  Handoff: "/handoff",
  Account: "/account",
  AccountSecurity: "/account/security",
} as const;

export const AuthenticationSearchParams = {
  ReturnUrl: "returnUrl",
  Request: "request",
} as const;
