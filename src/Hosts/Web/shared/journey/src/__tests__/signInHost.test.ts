import { describe, expect, it } from "vitest";
import { isSafeRelativeReturnTo, isSignInHost, readSignInUrl, signInHostRedirectUrl, signInUrlEnvironmentKey } from "../signInHost";
import { handoffUrl } from "../features/handoff/hostUrl";

const signInUrl = "https://auth.example.test";

describe("readSignInUrl", () => {
  it("reads the trimmed sign-in address and treats a blank or missing one as unset", () => {
    expect(readSignInUrl({ [signInUrlEnvironmentKey]: ` ${signInUrl} ` })).toBe(signInUrl);
    expect(readSignInUrl({ [signInUrlEnvironmentKey]: " " })).toBeNull();
    expect(readSignInUrl({})).toBeNull();
  });
});

describe("signInHostRedirectUrl", () => {
  it("sends a sign-in page on any other host to that page on the sign-in host", () => {
    expect(signInHostRedirectUrl(signInUrl, "rezaei.clinic.example.test", "/login")).toBe("https://auth.example.test/login");
  });

  it("keeps the sign-in host's own pages, whatever the case of the request host", () => {
    expect(signInHostRedirectUrl(signInUrl, "Auth.Example.Test", "/login")).toBeNull();
  });

  it("treats a different port as a different host", () => {
    expect(signInHostRedirectUrl("http://auth.localhost:3005", "auth.localhost:3006", "/login")).toBe("http://auth.localhost:3005/login");
  });

  it("stays on the sign-in host even for a path that looks like another host", () => {
    expect(new URL(signInHostRedirectUrl(signInUrl, "rezaei.clinic.example.test", "//other.example.test/login")!).host)
      .toBe("auth.example.test");
  });

  it("carries a safe relative return path to the sign-in host and drops anything else", () => {
    expect(signInHostRedirectUrl(signInUrl, "vitarish.example.test", "/login?returnUrl=%2Fstudio"))
      .toBe("https://auth.example.test/login?returnUrl=%2Fstudio");
    for (const unsafe of ["https://evil.example.test/", "//evil.example.test", "/\\evil.example.test", "/studio?x=1", "studio"]) {
      expect(signInHostRedirectUrl(signInUrl, "vitarish.example.test", `/login?returnUrl=${encodeURIComponent(unsafe)}`))
        .toBe("https://auth.example.test/login");
    }
  });
});

describe("isSignInHost", () => {
  it("matches only the sign-in host and port, whatever the case", () => {
    expect(isSignInHost(signInUrl, "AUTH.example.test")).toBe(true);
    expect(isSignInHost("http://auth.localhost:3005", "auth.localhost:3006")).toBe(false);
    expect(isSignInHost(signInUrl, null)).toBe(false);
  });
});

describe("isSafeRelativeReturnTo", () => {
  it("accepts only same-host paths without query, fragment or host tricks", () => {
    expect(isSafeRelativeReturnTo("/studio")).toBe(true);
    expect(isSafeRelativeReturnTo("/%2F%2Fevil.example.test")).toBe(false);
    for (const unsafe of [
      "/studio\tsettings",
      "/studio\nsettings",
      "/studio\rsettings",
      "/studio%09settings",
      "/studio%0Asettings",
      "/studio%0Dsettings",
      "%2F%09%2Fevil.example.test",
    ]) {
      expect(isSafeRelativeReturnTo(unsafe), unsafe).toBe(false);
    }
    expect(isSafeRelativeReturnTo("%")).toBe(false);
    expect(isSafeRelativeReturnTo(null)).toBe(false);
  });
});

describe("handoffUrl", () => {
  it("puts the token in the fragment and a return path in the query, on the target host", () => {
    Object.defineProperty(window, "location", { configurable: true, value: { href: "https://auth.example.test/continue?returnUrl=%2Fstudio" } });
    expect(handoffUrl("vitarish.example.test", "/handoff", "token-1", "/studio"))
      .toBe("https://vitarish.example.test/handoff?returnUrl=%2Fstudio#token-1");
    expect(handoffUrl("vitarish.example.test", "/handoff", "token-2")).toBe("https://vitarish.example.test/handoff#token-2");
  });
});
