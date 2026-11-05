/** Routes shared by the authentication surfaces in every Web app. */
export const AuthenticationWebRoutes = {
  SignIn: "/login",
  PasswordRecovery: "/login/forgot-password",
} as const;
