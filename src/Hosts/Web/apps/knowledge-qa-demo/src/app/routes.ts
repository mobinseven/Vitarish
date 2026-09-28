import { AuthenticationWebRoutes } from "@vitarish/auth/routes";

export const Routes = {
  Home: "/",
  Login: "/login",
  LoginForgotPassword: AuthenticationWebRoutes.PasswordRecovery,
  KnowledgeQa: "/knowledge/qa",
} as const;
