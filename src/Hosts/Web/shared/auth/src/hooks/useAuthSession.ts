"use client";

import { useMemo } from "react";
import { ApiError } from "@vitarish/api-client";
import {
  authenticationSessionGet,
  useAuthenticationSessionGet,
} from "../api/generated/authentication/authentication";
import type { AuthenticationSessionDto } from "../auth/authenticationTypes";
import { resolveUserDisplayProfile } from "../session/userDisplayProfile";

const anonymousSession: AuthenticationSessionDto = {
  isAuthenticated: false,
  user: null,
  tenantAccess: { canAccessTenantOwner: false, canAccessTenantUser: false },
  canAccessPlatformAdmin: false,
};

export function useAuthSession() {
  const query = useAuthenticationSessionGet({
    query: {
      queryFn: async ({ signal }) => {
        try {
          return await authenticationSessionGet(undefined, signal);
        } catch (error) {
          if (error instanceof ApiError && error.status === 401) {
            return anonymousSession;
          }

          throw error;
        }
      },
      retry: (failureCount) => failureCount < 1,
    },
  });

  const user = query.data?.user ?? null;
  const displayProfile = useMemo(() => resolveUserDisplayProfile(user), [user]);

  return {
    session: query.data,
    isAuthenticated: query.data?.isAuthenticated ?? false,
    user,
    displayProfile,
    isLoading: query.isLoading,
    isError: query.isError,
  };
}
