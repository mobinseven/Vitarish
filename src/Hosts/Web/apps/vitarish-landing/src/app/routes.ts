import { AuthenticationWebRoutes } from "@vitarish/auth/routes";

/** Public destinations owned by the Vitarish landing app. */
export const LandingRoutes = {
  Home: "/",
  Purpose: "/our-purpose",
  Studio: "/studio",
  SignIn: AuthenticationWebRoutes.SignIn,
  Darman: "https://darman.vitarish.com",
} as const;
