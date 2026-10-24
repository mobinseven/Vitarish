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

// Loading the barrel cold loads HeroUI and @vitarish/ui (several seconds on a busy machine),
// which is module loading time, not behaviour under test.
const coldBarrelImportTimeoutMilliseconds = 30_000;

describe("journey barrel exports", () => {
  it("exports key components", async () => {
    const mod = await import("../index");
    expect(mod.WorkspaceSelectionPage).toBeDefined();
    expect(mod.ProfileCompletionPage).toBeDefined();
    expect(mod.ContactConfirmationPage).toBeDefined();
    expect(mod.ConsentReviewPage).toBeDefined();
    expect(mod.TenantSetupPage).toBeDefined();
    expect(mod.TenantHandoffPage).toBeDefined();
    expect(mod.TenantLogoPrompt).toBeDefined();
    expect(mod.useCurrentJourney).toBeDefined();
  }, coldBarrelImportTimeoutMilliseconds);
});

describe("onboardingHandoffUrl", () => {
  it("keeps the ticket out of the path and query and keeps the onboarding host's own port", async () => {
    const { onboardingHandoffUrl } = await import("../onboardingHost");

    const url = new URL(onboardingHandoffUrl("http://start.localhost:3000/setup?x=1", "ticket-123"));

    expect(url.origin).toBe("http://start.localhost:3000");
    expect(url.pathname).toBe("/handoff");
    expect(url.search).toBe("");
    expect(url.hash).toBe("#ticket-123");
  });
});
