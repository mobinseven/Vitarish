import { describe, expect, it } from "vitest";
import { onboardingUrlEnvironmentKey, readOnboardingUrl, tenantOnboardingEnvironmentKey } from "../onboardingHost";

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
