import type { JourneyDecision } from "./useCurrentJourney";

/**
 * Resolves the post-login navigation destination from a Journey decision.
 * Falls back to legacy returnUrl if Journey is not available.
 */
export function resolvePostLoginNavigation(
  decision: JourneyDecision | undefined,
  legacyReturnUrl?: string
): string {
  if (decision?.step?.destinationUrl) {
    return decision.step.destinationUrl;
  }

  if (decision?.legacyReturnUrl) {
    return decision.legacyReturnUrl;
  }

  if (legacyReturnUrl) {
    return legacyReturnUrl;
  }

  return "/dashboard";
}
