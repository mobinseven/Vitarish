import { useCallback, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  useLogoutMutation,
  useGetLogoutContextQuery,
} from "@/api/generated/authentication/authentication";
import { ApiError } from "@/api/client/apiFetch";
import { Routes } from "@/app/routes";
import { parseAuthError, getErrorMessage, type AuthError } from "@/lib/errors";

export type LogoutContextStatus = "loading" | "ready" | "already-signed-out" | "unavailable";

export function useLogout(logoutId?: string) {
  const router = useRouter();
  const mutation = useLogoutMutation();
  const contextQuery = useGetLogoutContextQuery(logoutId ?? null);
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
          logoutId: logoutId ?? null,
          returnUrl: returnUrl ?? Routes.Login.Index,
        });
        if (result.redirectUrl) {
          router.push(result.redirectUrl);
        } else {
          router.push(Routes.Login.Index);
        }
      } catch (err) {
        setError(parseAuthError(err));
      }
    },
    [mutation, logoutId, router]
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
