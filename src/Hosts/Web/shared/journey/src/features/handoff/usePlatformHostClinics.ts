"use client";

import { ApiError } from "@vitarish/api-client";
import { usePartiesGetSignInClinics } from "../../api/generated/parties/parties";
import type { SignInClinicDto } from "../../api/generated/parties/model";

/**
 * The signed-in person's clinics when this host is a platform host that lists them (the clinic front page or the
 * sign-in host). On a clinic's own host the server does not list them, which reads as `clinic-host`.
 */
export type PlatformHostClinics =
  | { status: "loading" }
  | { status: "clinic-host" }
  | { status: "failed"; retry: () => void }
  | { status: "platform-host"; clinics: SignInClinicDto[]; createUrl: string | null };

export function usePlatformHostClinics(enabled: boolean): PlatformHostClinics {
  const query = usePartiesGetSignInClinics({ query: { enabled, retry: false, staleTime: 60_000 } });
  if (!enabled || query.isPending) return { status: "loading" };
  if (query.error) {
    return query.error instanceof ApiError && query.error.status === 404
      ? { status: "clinic-host" }
      : { status: "failed", retry: () => void query.refetch() };
  }
  if (!query.data) return { status: "loading" };
  return { status: "platform-host", clinics: query.data.clinics, createUrl: query.data.onboardingUrl ?? null };
}
