import { headers } from "next/headers";
import {
  readSignInUrl,
  signInHostRedirectUrl,
  signInUrlEnvironmentKey,
} from "@vitarish/journey/sign-in-host";
import { LandingRoutes } from "../routes";
import { ContentStudioAccess } from "./ContentStudioAccess";

export default async function StudioPage() {
  const requestHeaders = await headers();
  const signInUrl = readSignInUrl(process.env);
  if (!signInUrl) {
    throw new Error(`${signInUrlEnvironmentKey} must be configured for landing owner sign-in.`);
  }
  const loginHref = signInHostRedirectUrl(
    signInUrl,
    requestHeaders.get("host"),
    LandingRoutes.SignIn,
  );
  if (!loginHref) {
    throw new Error("The landing studio cannot use the shared sign-in host as its tenant host.");
  }

  return <ContentStudioAccess loginHref={loginHref} />;
}
