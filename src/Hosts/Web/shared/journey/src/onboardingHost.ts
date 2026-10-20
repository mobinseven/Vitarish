/**
 * The runtime environment variable naming the onboarding host's address, where people create
 * their own clinic. Hosts read it on the server per request, so one image serves every
 * environment; the deployment sets it from the same value as Main's `Vitarish:OnboardingUrl`.
 */
export const onboardingUrlEnvironmentKey = "VITARISH_ONBOARDING_URL";

/** The onboarding address from the server environment, or null when this deployment offers none. */
export function readOnboardingUrl(environment: Record<string, string | undefined>): string | null {
  const value = environment[onboardingUrlEnvironmentKey]?.trim();
  return value ? value : null;
}
