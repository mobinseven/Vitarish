"use client";

import { useCallback, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useAuthenticationLoginPassword, getAuthenticationSessionGetQueryKey } from "../api/generated/authentication/authentication";
import type { PasswordLoginRequest } from "../api/generated/authentication/model";
import { parseAuthError, getErrorMessage, type AuthError } from "../auth/errors";
import { getRequestedReturnUrl } from "./requestedReturnUrl";
import { ignoreAuthNavigation, type AuthNavigationOptions } from "./navigation";

export function usePasswordLogin({ navigate = ignoreAuthNavigation, returnUrlSearchParams }: AuthNavigationOptions = {}) {
  const mutation = useAuthenticationLoginPassword();
  const queryClient = useQueryClient();
  const [error, setError] = useState<AuthError | null>(null);

  const login = useCallback(
    async (
      userNameOrEmail: string,
      password: string,
      returnUrl?: string
    ) => {
      setError(null);
      try {
        const finalReturnUrl = getRequestedReturnUrl(returnUrlSearchParams, returnUrl);
        const data: Omit<PasswordLoginRequest, "returnUrl"> & Partial<Pick<PasswordLoginRequest, "returnUrl">> = {
          userNameOrEmail,
          password,
          rememberMe: false,
          ...(finalReturnUrl ? { returnUrl: finalReturnUrl } : {}),
        };

        const result = await mutation.mutateAsync({ data: data as PasswordLoginRequest });

        await queryClient.invalidateQueries({
          queryKey: getAuthenticationSessionGetQueryKey(),
        });

        // Use legacy returnUrl from auth result as fallback
        // Journey destination will be resolved by the gate boundary
        navigate(result.returnUrl);
      } catch (err) {
        const authError = parseAuthError(err);
        setError(authError);
      }
    },
    [mutation, navigate, queryClient, returnUrlSearchParams]
  );

  return {
    login,
    isLoading: mutation.isPending,
    error,
    errorMessage: error ? getErrorMessage(error) : null,
  };
}
