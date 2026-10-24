/**
 * The runtime environment variable naming the onboarding host's address, where people create
 * their own clinic. Hosts read it on the server per request, so one image serves every
 * environment; the deployment sets it from the same value as Main's `Vitarish:OnboardingUrl`.
 */
export const onboardingUrlEnvironmentKey = "VITARISH_ONBOARDING_URL";

/**
 * The runtime environment variable that switches clinic creation on (`true`); the deployment sets
 * it from the same value as Main's `Vitarish:Features:TenantOnboarding`.
 */
export const tenantOnboardingEnvironmentKey = "VITARISH_TENANT_ONBOARDING";

/** The onboarding address from the server environment, or null when onboarding is off or has no address. */
export function readOnboardingUrl(environment: Record<string, string | undefined>): string | null {
  if (environment[tenantOnboardingEnvironmentKey]?.trim().toLowerCase() !== "true") return null;
  const value = environment[onboardingUrlEnvironmentKey]?.trim();
  return value ? value : null;
}

/** Where a one-time handoff ticket is redeemed on the onboarding host and on every clinic host. */
export const tenantHandoffPath = "/handoff";

/**
 * The onboarding host's handoff address for a ticket. The ticket travels in the fragment, so it is
 * never sent to a server or in a `Referer` (ADR-0016).
 */
export function onboardingHandoffUrl(onboardingUrl: string, token: string): string {
  const url = new URL(tenantHandoffPath, onboardingUrl);
  url.hash = token;
  return url.toString();
}
