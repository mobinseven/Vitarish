"use client";

import { useMemo } from "react";
import { ApiError } from "@vitarish/api-client";
import { useAuthenticationSessionGet } from "../api/generated/authentication/authentication";
import { resolveUserDisplayProfile } from "../session/userDisplayProfile";

export function useAuthSession() {
  const query = useAuthenticationSessionGet({
    query: {
      retry: (failureCount, error) =>
        !(error instanceof ApiError && error.status === 401) && failureCount < 1,
    },
  });

  const unauthorized = query.error instanceof ApiError && query.error.status === 401;
  const session = unauthorized ? undefined : query.data;
  const user = session?.user ?? null;
  const displayProfile = useMemo(() => resolveUserDisplayProfile(user), [user]);

  return {
    session,
    isAuthenticated: session?.isAuthenticated ?? false,
    user,
    displayProfile,
    isLoading: query.isLoading && !unauthorized,
    isError: query.isError && !unauthorized,
  };
}
