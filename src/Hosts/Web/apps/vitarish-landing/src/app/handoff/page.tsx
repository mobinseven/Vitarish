import { LandingRoutes } from "../routes";
import { landingSignInHref } from "../signInHref";
import { LandingHandoff } from "./LandingHandoff";

/** Signs a person in on vitarish.com with the one-time link from the sign-in host. */
export default async function HandoffPage() {
  return <LandingHandoff signInHref={await landingSignInHref(LandingRoutes.PlatformHome)} />;
}
