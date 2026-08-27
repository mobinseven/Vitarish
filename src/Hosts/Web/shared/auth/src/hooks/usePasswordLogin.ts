"use client";

import { useCallback, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  useAuthenticationLoginPassword,
  useAuthenticationLoginPasswordOtpComplete,
  getAuthenticationSessionGetQueryKey,
} from "../api/generated/authentication/authentication";
import type { PasswordLoginRequest } from "../api/generated/authentication/model";
import { parseAuthError, getErrorMessage, type AuthError } from "../auth/errors";
import { getRequestedReturnUrl } from "./requestedReturnUrl";
import { ignoreAuthNavigation, type AuthNavigationOptions } from "./navigation";

export function usePasswordLogin({ navigate = ignoreAuthNavigation, returnUrlSearchParams }: AuthNavigationOptions = {}) {
  const mutation = useAuthenticationLoginPassword();
  const otpMutation = useAuthenticationLoginPasswordOtpComplete();
  const queryClient = useQueryClient();
  const [error, setError] = useState<AuthError | null>(null);
  const [otpChallengeId, setOtpChallengeId] = useState<string | null>(null);
  const [pendingReturnUrl, setPendingReturnUrl] = useState<string | undefined>(undefined);

  const login = useCallback(
    async (
      userNameOrEmail: string,
      password: string,
      returnUrl?: string
    ) => {
      setError(null);
      const finalReturnUrl = getRequestedReturnUrl(returnUrlSearchParams, returnUrl);
      try {
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
        if (authError.code === "additional_step_required" && authError.challengeId) {
          setOtpChallengeId(authError.challengeId);
          setPendingReturnUrl(finalReturnUrl);
          return;
        }
        setError(authError);
      }
    },
    [mutation, navigate, queryClient, returnUrlSearchParams]
  );

  const completeOtp = useCallback(async (code: string) => {
    if (!otpChallengeId) return;
    setError(null);
    try {
      const result = await otpMutation.mutateAsync({
        data: {
          challengeId: otpChallengeId,
          code,
          returnUrl: pendingReturnUrl ?? getRequestedReturnUrl(returnUrlSearchParams) ?? null,
        },
      });
      await queryClient.invalidateQueries({ queryKey: getAuthenticationSessionGetQueryKey() });
      navigate(result.returnUrl);
    } catch (err) {
      const authError = parseAuthError(err);
      setError(authError);
      if (
        authError.code === "challenge_expired" ||
        authError.code === "challenge_attempts_exhausted" ||
        authError.code === "challenge_already_used"
      ) {
        setOtpChallengeId(null);
        setPendingReturnUrl(undefined);
      }
    }
  }, [navigate, otpChallengeId, otpMutation, pendingReturnUrl, queryClient, returnUrlSearchParams]);

  return {
    login,
    completeOtp,
    requiresOtp: otpChallengeId !== null,
    isLoading: mutation.isPending || otpMutation.isPending,
    error,
    errorMessage: error ? getErrorMessage(error) : null,
  };
}
