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
 * Hook for tenant-based authorization.
 *
 * Currently checks authentication only.
 * When the API exposes role/tenant information, extend this hook
 * to check for specific tenant roles (owner, user).
 *
 * Policy: IdentityDefinitions.TenantOwner for /owner/clinic/**
 * Policy: IdentityDefinitions.TenantUser for /user/clinic/**
 */
export function useTenantAuthorization(
  _requiredRole?: TenantRole
): TenantAuthorizationResult {
  const { isAuthenticated, isLoading, user, displayProfile } = useAuthSession();

  const hasAccess = useMemo(() => {
    if (!isAuthenticated) {
      return false;
    }

    // TODO: When API exposes role/tenant information, check here:
    // if (requiredRole === "owner") {
    //   return user?.isTenantOwner === true;
    // }
    // if (requiredRole === "user") {
    //   return user?.isTenantUser === true;
    // }

    return true;
  }, [isAuthenticated]);

  return {
    isAuthenticated,
    isLoading,
    hasAccess,
    user,
    displayProfile,
  };
}
