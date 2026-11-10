import { describe, expect, it } from "vitest";
import { signInReturnUrlQueryKey } from "../../centralHost";
import { AuthenticationSearchParams, AuthenticationWebRoutes } from "../../routes";
import { authenticatedSignInRedirectOptions } from "../../components/SignInFrame";

const routes = { homeHref: "/app-home", continueHref: AuthenticationWebRoutes.Continue };

describe("authenticatedSignInRedirectOptions", () => {
  it("keeps an authenticated shared sign-in host visit on continue with a safe return intent", () => {
    const searchParams = new URLSearchParams(`${signInReturnUrlQueryKey}=%2Fstudio`);

    expect(authenticatedSignInRedirectOptions(true, searchParams, routes)).toEqual({
      authenticatedHref: `${routes.continueHref}?${signInReturnUrlQueryKey}=%2Fstudio`,
      returnUrlSearchParams: undefined,
    });
  });

  it("keeps a continuation request that the sign-in host issued", () => {
    const request = `${AuthenticationWebRoutes.Continue}?${AuthenticationSearchParams.Request}=abc`;

    expect(authenticatedSignInRedirectOptions(
      true,
      new URLSearchParams({ [signInReturnUrlQueryKey]: request }),
      routes,
    )).toEqual({ authenticatedHref: request, returnUrlSearchParams: undefined });
  });

  it("drops unsafe return intents before building the continue route", () => {
    expect(authenticatedSignInRedirectOptions(
      true,
      new URLSearchParams(`${signInReturnUrlQueryKey}=%2F%09%2Fevil.example`),
      routes,
    )).toEqual({ authenticatedHref: routes.continueHref, returnUrlSearchParams: undefined });
  });

  it("keeps standalone login on the app's home route and preserves its existing return intent", () => {
    const searchParams = new URLSearchParams(`${signInReturnUrlQueryKey}=%2Fstudio`);

    expect(authenticatedSignInRedirectOptions(false, searchParams, routes)).toEqual({
      authenticatedHref: routes.homeHref,
      returnUrlSearchParams: searchParams,
    });
  });
});
