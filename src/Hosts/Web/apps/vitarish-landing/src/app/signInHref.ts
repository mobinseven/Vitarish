import { headers } from "next/headers";
import {
  readSignInUrl,
  signInHostRedirectUrl,
  signInReturnUrlQueryKey,
  signInUrlEnvironmentKey,
} from "@vitarish/journey/sign-in-host";
import { LandingRoutes } from "./routes";

/** The shared sign-in host's address for this request, bringing the person back to `returnPath` here. */
export async function landingSignInHref(returnPath: string): Promise<string> {
  const signInUrl = readSignInUrl(process.env);
  if (!signInUrl) {
    throw new Error(`${signInUrlEnvironmentKey} must be configured for landing sign-in.`);
  }
  const href = signInHostRedirectUrl(
    signInUrl,
    (await headers()).get("host"),
    `${LandingRoutes.SignIn}?${signInReturnUrlQueryKey}=${encodeURIComponent(returnPath)}`,
  );
  if (!href) {
    throw new Error("The landing host cannot be the shared sign-in host.");
  }
  return href;
}
