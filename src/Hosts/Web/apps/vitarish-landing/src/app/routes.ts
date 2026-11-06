import { AuthenticationWebRoutes } from "@vitarish/auth/routes";
import { tenantHandoffPath } from "@vitarish/journey/onboarding-host";

/** Public destinations owned by the Vitarish landing app. */
export const LandingRoutes = {
  Home: "/",
  Purpose: "/our-purpose",
  Studio: "/studio",
  /** The signed-in person's platform home (ADR-0020). */
  PlatformHome: "/home",
  /** Redeems the one-time sign-in link from the sign-in host. */
  Handoff: tenantHandoffPath,
  SignIn: AuthenticationWebRoutes.SignIn,
  Darman: "https://darman.vitarish.com",
} as const;
