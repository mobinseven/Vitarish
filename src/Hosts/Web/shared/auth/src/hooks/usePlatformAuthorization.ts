"use client";

import { useAuthSession } from "./useAuthSession";

/** A client-side usability gate only; protected APIs remain authoritative. */
export function usePlatformAuthorization() {
  const { isAuthenticated, isLoading, session } = useAuthSession();
  // The generated authentication model is refreshed by the OpenAPI generator;
  // keep this compatibility cast until that authoritative output is regenerated.
  const platformSession = session as typeof session & { canAccessPlatformAdmin?: boolean };
  return { isAuthenticated, isLoading, hasAccess: isAuthenticated && platformSession?.canAccessPlatformAdmin === true };
}
