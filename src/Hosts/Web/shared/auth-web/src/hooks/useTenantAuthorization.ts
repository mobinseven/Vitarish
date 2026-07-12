"use client";

import { useMemo } from "react";
import { useAuthSession } from "./useAuthSession";

export type TenantRole = "owner" | "user";

interface TenantAuthorizationResult {
  isAuthenticated: boolean;
  isLoading: boolean;
  hasAccess: boolean;
  user: ReturnType<typeof useAuthSession>["user"];
  displayProfile: ReturnType<typeof useAuthSession>["displayProfile"];
}

/**
 * Client-side tenant authorization pre-check.
 *
 * Backend authorization remains authoritative; this hook only hides or redirects UI
 * before protected APIs enforce tenant access.
 */
export function useTenantAuthorization(
  requiredRole?: TenantRole
): TenantAuthorizationResult {
  const { session, isAuthenticated, isLoading, user, displayProfile } = useAuthSession();

  const hasAccess = useMemo(() => {
    if (!isAuthenticated) {
      return false;
    }

    if (requiredRole === "owner") {
      return session?.tenantAccess?.canAccessTenantOwner === true;
    }

    if (requiredRole === "user") {
      return session?.tenantAccess?.canAccessTenantUser === true;
    }

    return true;
  }, [isAuthenticated, requiredRole, session?.tenantAccess?.canAccessTenantOwner, session?.tenantAccess?.canAccessTenantUser]);

  return {
    isAuthenticated,
    isLoading,
    hasAccess,
    user,
    displayProfile,
  };
}
