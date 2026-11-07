import { AuthenticationWebRoutes } from "@vitarish/auth/routes";

export const Routes = {
  Root: "/",
  Home: "/",
  Login: { Index: AuthenticationWebRoutes.SignIn, Password: "/login/password", Phone: "/login/phone", Email: "/login/email", Passkey: "/login/passkey", ForgotPassword: AuthenticationWebRoutes.PasswordRecovery },
  Logout: "/logout",
  Directory: "/practices",
  Profile: "/profile",
  Appointments: "/appointments",
  Reminders: "/reminders",
  Records: "/records",
  Prescriptions: "/prescriptions",
  Documents: "/documents",
} as const;
