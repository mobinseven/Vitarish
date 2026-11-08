import { AuthenticationSearchParams } from "@vitarish/auth/routes";
import { LandingRoutes } from "./routes";

/** Begin on the originating platform host so the Auth return is browser-correlated. */
export async function landingSignInHref(returnPath: string): Promise<string> {
  return `${LandingRoutes.SignIn}?${new URLSearchParams({ [AuthenticationSearchParams.ReturnUrl]: returnPath })}`;
}
