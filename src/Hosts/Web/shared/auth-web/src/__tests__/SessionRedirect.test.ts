import { describe, expect, it } from "vitest";
import { resolveSessionRedirectTarget } from "../components/SessionRedirect";

describe("resolveSessionRedirectTarget", () => {
  it("does not redirect while the session is still loading", () => {
    expect(
      resolveSessionRedirectTarget({
        isLoading: true,
        isAuthenticated: false,
      })
    ).toBeNull();
  });

  it("sends authenticated root visitors to the home dashboard", () => {
    expect(
      resolveSessionRedirectTarget({
        isLoading: false,
        isAuthenticated: true,
        authenticatedHref: "/home",
      })
    ).toBe("/home");
  });

  it("sends anonymous root visitors to login", () => {
    expect(
      resolveSessionRedirectTarget({
        isLoading: false,
        isAuthenticated: false,
        anonymousHref: "/login",
      })
    ).toBe("/login");
  });

  it("lets anonymous visitors stay on public auth routes", () => {
    expect(
      resolveSessionRedirectTarget({
        isLoading: false,
        isAuthenticated: false,
        anonymousHref: null,
      })
    ).toBeNull();
  });

  it("moves authenticated visitors away from public auth routes", () => {
    expect(
      resolveSessionRedirectTarget({
        isLoading: false,
        isAuthenticated: true,
        authenticatedHref: "/home",
        anonymousHref: null,
      })
    ).toBe("/home");
  });
});
