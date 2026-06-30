import { getRequestedReturnUrl } from "@/hooks/auth/requestedReturnUrl";

describe("getRequestedReturnUrl", () => {
  it.each([
    undefined,
    "",
    "/",
    "/login",
    "/login/password",
    "/login?foo=bar",
    "/login#hash",
    "/logout",
    "/logout/callback",
    "/logout?foo=bar",
  ])(
    "ignores auth-only or empty return URL %s",
    (explicitReturnUrl) => {
      expect(
        getRequestedReturnUrl(new URLSearchParams(), explicitReturnUrl)
      ).toBeUndefined();
    }
  );

  it.each(["  /login  ", " /logout "])(
    "trims whitespace before evaluating %s",
    (explicitReturnUrl) => {
      expect(
        getRequestedReturnUrl(new URLSearchParams(), explicitReturnUrl)
      ).toBeUndefined();
    }
  );

  it("uses the query string return URL when no explicit value exists", () => {
    expect(
      getRequestedReturnUrl(new URLSearchParams("returnUrl=/account"))
    ).toBe("/account");
  });

  it("prefers the explicit return URL over the query string value", () => {
    expect(
      getRequestedReturnUrl(
        new URLSearchParams("returnUrl=/account"),
        "/dashboard"
      )
    ).toBe("/dashboard");
  });
});
