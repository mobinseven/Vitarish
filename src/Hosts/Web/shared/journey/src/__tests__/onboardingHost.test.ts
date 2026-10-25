import { describe, expect, it } from "vitest";
import { isOnboardingHost, onboardingUrlEnvironmentKey, readOnboardingUrl, tenantOnboardingEnvironmentKey } from "../onboardingHost";

const onboardingUrl = "https://start.clinic.example.test";

describe("readOnboardingUrl", () => {
  it("offers the onboarding address while tenant onboarding is on", () => {
    expect(
      readOnboardingUrl({ [tenantOnboardingEnvironmentKey]: "true", [onboardingUrlEnvironmentKey]: ` ${onboardingUrl} ` }),
    ).toBe(onboardingUrl);
  });

  it("hides the onboarding address while tenant onboarding is off or unset, even when the address is set", () => {
    expect(readOnboardingUrl({ [tenantOnboardingEnvironmentKey]: "false", [onboardingUrlEnvironmentKey]: onboardingUrl })).toBeNull();
    expect(readOnboardingUrl({ [onboardingUrlEnvironmentKey]: onboardingUrl })).toBeNull();
  });

  it("hides the onboarding address when tenant onboarding is on but no address is set", () => {
    expect(readOnboardingUrl({ [tenantOnboardingEnvironmentKey]: "true", [onboardingUrlEnvironmentKey]: " " })).toBeNull();
  });
});

describe("isOnboardingHost", () => {
  it("recognizes the onboarding host whatever the case of the request host", () => {
    expect(isOnboardingHost("Start.Clinic.Example.Test", onboardingUrl)).toBe(true);
  });

  it("tells a clinic host, a different port and a missing host apart from the onboarding host", () => {
    expect(isOnboardingHost("rezaei.clinic.example.test", onboardingUrl)).toBe(false);
    expect(isOnboardingHost("start.localhost:3001", "http://start.localhost:3000")).toBe(false);
    expect(isOnboardingHost(null, onboardingUrl)).toBe(false);
  });
});
