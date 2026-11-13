import { describe, expect, it } from "vitest";
import { signInHrefReturningTo } from "../routes";

describe("signInHrefReturningTo", () => {
  it.each([null, undefined, ""])("returns the plain sign-in href when there is no return path (%j)", (returnPath) => {
    expect(signInHrefReturningTo(returnPath)).toBe("/login");
  });

  it.each(["//evil.com", "https://evil.com", "/\\evil"])("leaves out a return path that is not a local path (%s)", (returnPath) => {
    expect(signInHrefReturningTo(returnPath)).toBe("/login");
  });

  it("carries the current page path and query as the encoded return target", () => {
    expect(signInHrefReturningTo("/patients/1?tab=x")).toBe("/login?returnUrl=%2Fpatients%2F1%3Ftab%3Dx");
  });

  it("appends the return target with & when the sign-in href already has a query", () => {
    expect(signInHrefReturningTo("/patients/1", "/login?lang=en")).toBe("/login?lang=en&returnUrl=%2Fpatients%2F1");
  });
});
