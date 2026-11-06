import { PlatformHome } from "../../features/home/PlatformHome";
import { LandingRoutes } from "../routes";
import { landingSignInHref } from "../signInHref";

/** The signed-in person's platform home on vitarish.com (ADR-0020). */
export default async function HomePage() {
  return <PlatformHome signInHref={await landingSignInHref(LandingRoutes.PlatformHome)} />;
}
