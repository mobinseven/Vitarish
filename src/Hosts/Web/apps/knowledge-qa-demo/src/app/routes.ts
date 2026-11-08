import { AuthenticationWebRoutes } from "@vitarish/auth/routes";

export const Routes = {
  Home: "/",
  Login: AuthenticationWebRoutes.SignIn,
  LoginForgotPassword: AuthenticationWebRoutes.PasswordRecovery,
  KnowledgeQa: "/knowledge/qa",
} as const;
