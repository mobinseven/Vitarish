import { AccountPage } from "../../features/account/AccountPage";
import { LandingRoutes } from "../routes";
import { landingSignInHref } from "../signInHref";

/** The signed-in person's account area on vitarish.com: sign-in settings, email and phone. */
export default async function AccountRoutePage() {
  // Signing in again returns to the platform home, which links here.
  return <AccountPage signInHref={await landingSignInHref(LandingRoutes.PlatformHome)} />;
}
