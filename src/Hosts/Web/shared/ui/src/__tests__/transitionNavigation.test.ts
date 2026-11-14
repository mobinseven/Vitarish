import { afterEach, describe, expect, it, vi } from "vitest";
import { carryNavigationTransition, clearNavigationTransition, takeNavigationTransition } from "../navigation/transitionNavigation";
import { navigationTransitionQueryKey, navigationTransitionStorageKey } from "../navigation/transitionContext";

afterEach(() => {
  vi.restoreAllMocks();
  clearNavigationTransition();
  window.history.replaceState(null, "", "/");
});

describe("per-tab presentation capture", () => {
  it("scrubs UI metadata, retains correlation request and ticket, and carries the same attempt", () => {
    const context = { version: 1, attempt: "352c2d23-842e-478d-800a-f64232ce1ae3", started: Date.now(), mode: "dark", locale: "fa" };
    const query = new URLSearchParams({ request: "opaque", [navigationTransitionQueryKey]: JSON.stringify(context) });
    window.history.replaceState({ preserved: true }, "", `/continue?${query}#opaque-ticket`);
    expect(takeNavigationTransition()).toEqual(context);
    expect(window.location.search).toBe("?request=opaque");
    expect(window.location.hash).toBe("#opaque-ticket");
    expect(window.history.state).toEqual({ preserved: true });
    const target = new URL(carryNavigationTransition("https://app.example.test/handoff#another-ticket"));
    expect(JSON.parse(target.searchParams.get(navigationTransitionQueryKey)!)).toEqual(context);
    expect(target.hash).toBe("#another-ticket");
    clearNavigationTransition();
    expect(window.sessionStorage.getItem(navigationTransitionStorageKey)).toBeNull();
  });

  it("still navigates with blocked optional storage", () => {
    vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => { throw new DOMException("denied"); });
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => { throw new DOMException("denied"); });
    vi.spyOn(Storage.prototype, "removeItem").mockImplementation(() => { throw new DOMException("denied"); });
    expect(carryNavigationTransition("/home")).toBe("/home");
    expect(() => clearNavigationTransition()).not.toThrow();
  });

  it("retains same-origin handover storage but removes the origin copy when crossing origins", () => {
    const attempt = takeNavigationTransition();
    carryNavigationTransition("/home");
    expect(JSON.parse(sessionStorage.getItem(navigationTransitionStorageKey)!)).toEqual(attempt);
    carryNavigationTransition("https://auth.example.test/login");
    expect(sessionStorage.getItem(navigationTransitionStorageKey)).toBeNull();
  });
});
