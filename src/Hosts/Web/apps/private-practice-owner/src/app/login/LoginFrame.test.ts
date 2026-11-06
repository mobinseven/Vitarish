import { describe, expect, it } from "vitest";
import { Routes } from "@/app/routes";
import { signInReturnUrlQueryKey } from "@vitarish/journey/sign-in-host";
import { authenticatedLoginRedirectOptions } from "./LoginFrame";

describe("authenticatedLoginRedirectOptions", () => {
  it("keeps an authenticated shared sign-in host visit on continue with a safe return intent", () => {
    const searchParams = new URLSearchParams(`${signInReturnUrlQueryKey}=%2Fstudio`);

    expect(authenticatedLoginRedirectOptions(true, searchParams)).toEqual({
      authenticatedHref: `${Routes.SignInContinue}?${signInReturnUrlQueryKey}=%2Fstudio`,
      returnUrlSearchParams: undefined,
    });
  });

  it("drops unsafe return intents before building the continue route", () => {
    expect(authenticatedLoginRedirectOptions(
      true,
      new URLSearchParams(`${signInReturnUrlQueryKey}=%2F%09%2Fevil.example`),
    )).toEqual({ authenticatedHref: Routes.SignInContinue, returnUrlSearchParams: undefined });
  });

  it("keeps standalone login on its clinic home route and preserves its existing return intent", () => {
    const searchParams = new URLSearchParams(`${signInReturnUrlQueryKey}=%2Fstudio`);

    expect(authenticatedLoginRedirectOptions(false, searchParams)).toEqual({
      authenticatedHref: Routes.Home,
      returnUrlSearchParams: searchParams,
    });
  });
});
