"use client";

import { useCallback, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  useAuthenticationLoginPassword,
  useAuthenticationLoginPasswordOtpComplete,
  useAuthenticationLoginPasswordOtpResend,
  getAuthenticationSessionGetQueryKey,
} from "../api/generated/authentication/authentication";
import type { AuthenticationChallengeDto, PasswordLoginRequest } from "../api/generated/authentication/model";
import { isExpiredOrExhaustedChallenge, parseAuthError, getErrorMessage, type AuthError } from "../auth/errors";
import { getRequestedReturnUrl } from "./requestedReturnUrl";
import { ignoreAuthNavigation, type AuthNavigationOptions } from "./navigation";

export function usePasswordLogin({ navigate = ignoreAuthNavigation, returnUrlSearchParams }: AuthNavigationOptions = {}) {
  const mutation = useAuthenticationLoginPassword();
  const otpMutation = useAuthenticationLoginPasswordOtpComplete();
  const resendOtpMutation = useAuthenticationLoginPasswordOtpResend();
  const queryClient = useQueryClient();
  const [error, setError] = useState<AuthError | null>(null);
  const [challenge, setChallenge] = useState<{
    challengeId: string;
    usesEmailSecondFactor?: boolean;
    maskedDestination?: string;
    expiresAt?: string;
    remainingAttempts?: AuthenticationChallengeDto["remainingAttempts"];
  } | null>(null);
  const [statusCode, setStatusCode] = useState<string | null>(null);
  const [pendingReturnUrl, setPendingReturnUrl] = useState<string | undefined>(undefined);

  const login = useCallback(
    async (
      userNameOrEmail: string,
      password: string,
      returnUrl?: string
    ) => {
      setError(null);
      setStatusCode(null);
      const finalReturnUrl = getRequestedReturnUrl(returnUrlSearchParams, returnUrl);
      try {
        const data: Omit<PasswordLoginRequest, "returnUrl"> & Partial<Pick<PasswordLoginRequest, "returnUrl">> = {
          userNameOrEmail,
          password,
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
          setChallenge({
            challengeId: authError.challengeId,
            usesEmailSecondFactor: authError.usesEmailSecondFactor,
            maskedDestination: authError.maskedDestination,
            expiresAt: authError.challengeExpiresAt,
            remainingAttempts: authError.remainingAttempts,
          });
          setPendingReturnUrl(finalReturnUrl);
          return;
        }
        setError(authError);
      }
    },
    [mutation, navigate, queryClient, returnUrlSearchParams]
  );

  const completeOtp = useCallback(async (code: string) => {
    if (!challenge) return;
    setError(null);
    setStatusCode(null);
    try {
      const result = await otpMutation.mutateAsync({
        data: {
          challengeId: challenge.challengeId,
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
        isExpiredOrExhaustedChallenge(authError.code) ||
        authError.code === "challenge_already_used"
      ) {
        setChallenge(null);
        setPendingReturnUrl(undefined);
      }
    }
  }, [navigate, challenge, otpMutation, pendingReturnUrl, queryClient, returnUrlSearchParams]);

  const resendOtp = useCallback(async () => {
    if (!challenge) return;
    setError(null);
    setStatusCode(null);
    try {
      const result = await resendOtpMutation.mutateAsync({
        data: { challengeId: challenge.challengeId },
      });
      setChallenge((current) => current ? {
        ...current,
        expiresAt: result.expiresAt,
        remainingAttempts: result.remainingAttempts,
      } : current);
      setStatusCode("auth.passwordLoginOtp.codeResent");
    } catch (err) {
      const authError = parseAuthError(err);
      setStatusCode(null);
      setError(authError);
      if (isExpiredOrExhaustedChallenge(authError.code)) {
        setChallenge(null);
        setPendingReturnUrl(undefined);
      }
    }
  }, [challenge, resendOtpMutation]);

  const resetOtp = useCallback(() => {
    setChallenge(null);
    setError(null);
    setStatusCode(null);
    setPendingReturnUrl(undefined);
  }, []);

  return {
    login,
    completeOtp,
    resendOtp,
    resetOtp,
    challenge,
    requiresOtp: challenge !== null,
    isLoading: mutation.isPending || otpMutation.isPending || resendOtpMutation.isPending,
    error,
    errorMessage: error ? getErrorMessage(error) : null,
    statusCode,
  };
}
