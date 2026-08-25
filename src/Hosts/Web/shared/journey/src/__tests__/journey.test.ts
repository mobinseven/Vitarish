import { describe, expect, it } from "vitest";
import { resolvePostLoginNavigation } from "../hooks/resolvePostLoginNavigation";

describe("resolvePostLoginNavigation", () => {
  it("returns default destination when no decision", () => {
    const result = resolvePostLoginNavigation(undefined, undefined, "/home");
    expect(result).toBe("/home");
  });

  it("returns legacy returnUrl when no journey decision", () => {
    const result = resolvePostLoginNavigation(undefined, "/dashboard", "/home");
    expect(result).toBe("/dashboard");
  });

  it("returns destinationUrl from journey step", () => {
    const decision = {
      step: { destinationUrl: "/workspace/select", destinationKind: "Gate" as const },
    };
    const result = resolvePostLoginNavigation(decision, undefined, "/home");
    expect(result).toBe("/workspace/select");
  });

  it("prefers journey step over legacy returnUrl", () => {
    const decision = {
      step: { destinationUrl: "/workspace/select", destinationKind: "Gate" as const },
      legacyReturnUrl: "/old-dashboard",
    };
    const result = resolvePostLoginNavigation(decision, "/legacy", "/home");
    expect(result).toBe("/workspace/select");
  });
});

describe("journey barrel exports", () => {
  it("exports key components", async () => {
    const mod = await import("../index");
    expect(mod.WorkspaceSelectionPage).toBeDefined();
    expect(mod.ProfileCompletionPage).toBeDefined();
    expect(mod.ContactConfirmationPage).toBeDefined();
    expect(mod.ConsentReviewPage).toBeDefined();
    expect(mod.useCurrentJourney).toBeDefined();
  });
});
