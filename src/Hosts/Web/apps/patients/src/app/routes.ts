export const Routes = {
  Root: "/",
  Home: "/",
  Login: { Index: "/login", Password: "/login/password", Phone: "/login/phone", Email: "/login/email", Passkey: "/login/passkey" },
  Logout: "/logout",
  Directory: "/practices",
  Profile: "/profile",
  Appointments: "/appointments",
  Reminders: "/reminders",
  Records: "/records",
  Prescriptions: "/prescriptions",
  Documents: "/documents",
} as const;
