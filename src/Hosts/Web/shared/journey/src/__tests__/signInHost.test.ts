import { describe, expect, it } from "vitest";
import { readSignInUrl, signInHostRedirectUrl, signInUrlEnvironmentKey } from "../signInHost";

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
});
