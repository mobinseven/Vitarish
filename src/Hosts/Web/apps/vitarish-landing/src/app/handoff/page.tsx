import { LandingRoutes } from "../routes";
import { landingSignInHref } from "../signInHref";
import { isSafeRelativeReturnTo, signInReturnUrlQueryKey } from "@vitarish/journey/sign-in-host";
import { LandingHandoff } from "./LandingHandoff";

/** Signs a person in on vitarish.com with the one-time link from the sign-in host. */
export default async function HandoffPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const queryValue = (await searchParams)[signInReturnUrlQueryKey];
  const requestedReturnUrl = typeof queryValue === "string" ? queryValue : undefined;
  const returnUrl = requestedReturnUrl && isSafeRelativeReturnTo(requestedReturnUrl)
    ? requestedReturnUrl
    : LandingRoutes.PlatformHome;
  return <LandingHandoff signInHref={await landingSignInHref(returnUrl)} destination={returnUrl} />;
}
