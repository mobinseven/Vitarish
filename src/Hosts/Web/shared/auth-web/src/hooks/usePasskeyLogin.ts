"use client";

import { useCallback, useState } from "react";
import {
  useAuthenticationLoginPasskeyBegin,
  useAuthenticationLoginPasskeyComplete,
} from "../api/generated/authentication/authentication";
import { startPasskeyAuthentication } from "../webauthn";
import { parseAuthError, getErrorMessage, type AuthError } from "../auth/errors";
import { getRequestedReturnUrl } from "./requestedReturnUrl";
import { ignoreAuthNavigation, type AuthNavigationOptions } from "./navigation";

export function usePasskeyLogin({ navigate = ignoreAuthNavigation, returnUrlSearchParams }: AuthNavigationOptions = {}) {
  const beginMutation = useAuthenticationLoginPasskeyBegin();
  const completeMutation = useAuthenticationLoginPasskeyComplete();
  const [error, setError] = useState<AuthError | null>(null);

  const login = useCallback(
    async (userNameOrEmail?: string, returnUrl?: string) => {
      setError(null);
      try {
        const finalReturnUrl = getRequestedReturnUrl(returnUrlSearchParams, returnUrl);
        const challenge = await beginMutation.mutateAsync({
          data: { userNameOrEmail: userNameOrEmail || null, returnUrl: finalReturnUrl ?? null },
        });

        if (!challenge.optionsJson) {
          throw new Error("No passkey options received");
        }

        const credentialJson = await startPasskeyAuthentication(
          challenge.optionsJson
        );

        const result = await completeMutation.mutateAsync({
          data: { challengeId: challenge.challengeId, credentialJson, returnUrl: finalReturnUrl ?? null },
        });

        navigate(result.returnUrl);
      } catch (err) {
        const authError = parseAuthError(err);
        setError(authError);
      }
    },
    [beginMutation, completeMutation, navigate, returnUrlSearchParams]
  );

  return {
    login,
    isLoading: beginMutation.isPending || completeMutation.isPending,
    error,
    errorMessage: error ? getErrorMessage(error) : null,
  };
}
