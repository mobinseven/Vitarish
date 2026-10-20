import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@vitarish/api-client";

export interface JourneyGate {
  kind:
    | "ContactConfirmation"
    | "ProfileCompletion"
    | "RequiredConsent"
    | "WorkspaceSelection"
    | "AccountRecovery"
    | "SuspiciousLogin"
    | "PasskeyEnrollmentPrompt"
    | "MfaEnrollmentPrompt"
    | "TenantCreation";
  isBlocking: boolean;
  destinationUrl: string;
  message?: string;
}

export interface JourneyStep {
  destinationUrl: string;
  destinationKind:
    | "Gate"
    | "ReturnUrl"
    | "AppDashboard"
    | "UserHome"
    | "GlobalDashboard";
  blockingGate?: JourneyGate;
  nonBlockingGates?: JourneyGate[];
}

export interface JourneyDecision {
  step: JourneyStep;
  legacyReturnUrl?: string;
  legacyRequiresProfileCompletion?: boolean;
}

async function fetchCurrentJourney(
  returnUrl?: string
): Promise<JourneyDecision> {
  const params = new URLSearchParams();
  if (returnUrl) {
    params.set("returnUrl", returnUrl);
  }
  const queryString = params.toString();
  const url = `/api/modules/journey/current${queryString ? `?${queryString}` : ""}`;

  return apiFetch<JourneyDecision>(url);
}

export function useCurrentJourney(returnUrl?: string) {
  return useQuery({
    queryKey: ["journey", "current", returnUrl],
    queryFn: () => fetchCurrentJourney(returnUrl),
    staleTime: 30_000,
    retry: false,
  });
}
