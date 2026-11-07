import { LandingRoutes } from "../routes";
import { landingSignInHref } from "../signInHref";
import { ContentStudioAccess } from "./ContentStudioAccess";

export default async function StudioPage() {
  return <ContentStudioAccess loginHref={await landingSignInHref(LandingRoutes.Studio)} />;
}
