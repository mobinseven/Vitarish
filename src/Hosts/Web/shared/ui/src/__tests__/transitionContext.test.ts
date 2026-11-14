import { describe, expect, it } from "vitest";
import { navigationTransitionPolicy, navigationTransitionQueryKey, parseNavigationTransition, withNavigationTransition, withoutNavigationTransition, type NavigationTransitionContext } from "../navigation/transitionContext";

const now = 1_800_000_000_000;
const context: NavigationTransitionContext = { version: 1, attempt: "352c2d23-842e-478d-800a-f64232ce1ae3", started: now, mode: "dark", locale: "fa" };
const base = "https://app.example.test";

describe("presentation transition envelope", () => {
  it("accepts only bounded current presentation fields", () => {
    expect(parseNavigationTransition(JSON.stringify(context), now)).toEqual(context);
    for (const change of [{ version: 2 }, { started: now + 1 }, { started: now - navigationTransitionPolicy.maximumAgeMilliseconds }, { locale: "fr" }, { mode: "system" }, { attempt: "not-an-attempt" }, { token: "must-not-be-carried" }]) {
      expect(parseNavigationTransition(JSON.stringify({ ...context, ...change }), now)).toBeNull();
    }
    for (const invalid of [null, "{", "[]", "null", "x".repeat(navigationTransitionPolicy.maximumEnvelopeLength + 1)]) {
      expect(parseNavigationTransition(invalid, now)).toBeNull();
    }
  });

  it("preserves the approved origin/path, request and opaque ticket through adding and stripping", () => {
    const target = "https://auth.example.test/continue?request=opaque%2Brequest#opaque%2Bticket";
    const carried = withNavigationTransition(target, context, base);
    const url = new URL(carried);
    expect(url.origin + url.pathname).toBe("https://auth.example.test/continue");
    expect(url.searchParams.get("request")).toBe("opaque+request");
    expect(url.hash).toBe("#opaque%2Bticket");
    expect(parseNavigationTransition(url.searchParams.get(navigationTransitionQueryKey), now)).toEqual(context);
    expect(withoutNavigationTransition(carried)).toBe(target);
  });

  it("strips only presentation metadata from local API return paths", () => {
    const carried = new URL(withNavigationTransition("/continue?request=opaque#ticket", context, base));
    expect(withoutNavigationTransition(carried.pathname + carried.search + carried.hash)).toBe("/continue?request=opaque#ticket");
    expect(withoutNavigationTransition("/home?filter=a%20b#section")).toBe("/home?filter=a%20b#section");
    const exact = "/continue?returnUrl=/a%20b~c&k=1#opaque%2Bticket";
    expect(withoutNavigationTransition(withNavigationTransition(exact, context, base))).toBe(base + exact);
    for (const unsafe of ["//external.test/path", "/\\external.test/path", "https://external.test/a/../b"]) {
      expect(withoutNavigationTransition(`${unsafe}?${navigationTransitionQueryKey}=invalid#fragment`)).toBe(unsafe + "#fragment");
    }
  });
});
