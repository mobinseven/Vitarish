import { AuthenticationWebRoutes } from "@vitarish/auth/routes";

export const Routes = {
  Root: "/",
  Home: "/",
  Login: { Index: AuthenticationWebRoutes.SignIn, Password: AuthenticationWebRoutes.SignInPassword, Phone: AuthenticationWebRoutes.SignInPhone, Email: AuthenticationWebRoutes.SignInEmail, Passkey: AuthenticationWebRoutes.SignInPasskey, ForgotPassword: AuthenticationWebRoutes.PasswordRecovery },
  Logout: "/logout",
  Directory: "/practices",
  Profile: "/profile",
  Appointments: "/appointments",
  Reminders: "/reminders",
  Records: "/records",
  Prescriptions: "/prescriptions",
  Documents: "/documents",
} as const;
