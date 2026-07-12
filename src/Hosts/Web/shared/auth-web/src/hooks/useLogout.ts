"use client";

import { useCallback, useMemo, useState } from "react";
import {
  getAuthenticationSessionGetQueryKey,
  useAuthenticationLogoutPost,
  useAuthenticationLogoutContext,
} from "../api/generated/authentication/authentication";
import { useQueryClient } from "@tanstack/react-query";
import { ApiError } from "@vitarish/web-api-client";
import { parseAuthError, getErrorMessage, type AuthError } from "../auth/errors";
import { ignoreAuthNavigation, type AuthNavigate } from "./navigation";

export type LogoutContextStatus = "loading" | "ready" | "already-signed-out" | "unavailable";

export function useLogout(logoutId?: string, fallbackLoginHref = "/login", navigate: AuthNavigate = ignoreAuthNavigation) {
  const mutation = useAuthenticationLogoutPost();
  const contextQuery = useAuthenticationLogoutContext(logoutId ? { logoutId } : undefined);
  const queryClient = useQueryClient();
  const [error, setError] = useState<AuthError | null>(null);

  const contextStatus = useMemo<LogoutContextStatus>(() => {
    if (contextQuery.isLoading) {
      return "loading";
    }

    if (contextQuery.error instanceof ApiError && contextQuery.error.status === 401) {
      return "already-signed-out";
    }

    if (contextQuery.isError) {
      return "unavailable";
    }

    return "ready";
  }, [contextQuery.isLoading, contextQuery.error, contextQuery.isError]);

  const logout = useCallback(
    async (returnUrl?: string) => {
      setError(null);
      try {
        const result = await mutation.mutateAsync({
          data: { logoutId: logoutId ?? null, returnUrl: returnUrl ?? fallbackLoginHref },
        });
        queryClient.removeQueries({ queryKey: getAuthenticationSessionGetQueryKey() });
        if (result.redirectUrl) {
          navigate(result.redirectUrl);
        } else {
          navigate(fallbackLoginHref);
        }
      } catch (err) {
        setError(parseAuthError(err));
      }
    },
    [mutation, logoutId, fallbackLoginHref, navigate, queryClient]
  );

  return {
    logout,
    context: contextQuery.data,
    contextStatus,
    isContextLoading: contextQuery.isLoading,
    isLoading: mutation.isPending,
    error,
    errorMessage: error ? getErrorMessage(error) : null,
  };
}
