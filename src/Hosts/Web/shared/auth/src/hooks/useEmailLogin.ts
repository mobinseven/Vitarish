"use client";

import { useCallback, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  useAuthenticationLoginEmailBegin,
  useAuthenticationLoginEmailComplete,
  getAuthenticationSessionGetQueryKey,
} from "../api/generated/authentication/authentication";
import type { AuthenticationChallengeDto } from "../api/generated/authentication/model";
import { parseAuthError, getErrorMessage, type AuthError } from "../auth/errors";
import { getRequestedReturnUrl } from "./requestedReturnUrl";
import { ignoreAuthNavigation, type AuthNavigationOptions } from "./navigation";

export type EmailLoginStep = "email" | "code";

export function useEmailLogin({ navigate = ignoreAuthNavigation, returnUrlSearchParams }: AuthNavigationOptions = {}) {
  const beginMutation = useAuthenticationLoginEmailBegin();
  const completeMutation = useAuthenticationLoginEmailComplete();
  const queryClient = useQueryClient();
  const [step, setStep] = useState<EmailLoginStep>("email");
  const [challenge, setChallenge] = useState<AuthenticationChallengeDto | null>(
    null
  );
  const [error, setError] = useState<AuthError | null>(null);
  const [statusCode, setStatusCode] = useState<string | null>(null);
  const [email, setEmail] = useState("");
  const [pendingReturnUrl, setPendingReturnUrl] = useState<string | undefined>(
    undefined
  );

  const begin = useCallback(
    async (emailAddr: string, returnUrl?: string) => {
      setError(null);
      setStatusCode(null);
      try {
        const finalReturnUrl = getRequestedReturnUrl(returnUrlSearchParams, returnUrl);
        setPendingReturnUrl(finalReturnUrl ?? undefined);
        const result = await beginMutation.mutateAsync({
          data: { email: emailAddr, returnUrl: finalReturnUrl ?? null },
        });
        setChallenge(result);
        setEmail(emailAddr);
        setStep("code");
      } catch (err) {
        setStatusCode(null);
        setError(parseAuthError(err));
      }
    },
    [beginMutation, returnUrlSearchParams]
  );

  const complete = useCallback(
    async (code: string) => {
      if (!challenge) return;
      setError(null);
      setStatusCode(null);
      try {
        const returnUrl =
          pendingReturnUrl ?? getRequestedReturnUrl(returnUrlSearchParams);
        const result = await completeMutation.mutateAsync({
          data: { challengeId: challenge.challengeId, code, returnUrl: returnUrl ?? null },
        });
        await queryClient.invalidateQueries({ queryKey: getAuthenticationSessionGetQueryKey() });
        navigate(result.returnUrl);
      } catch (err) {
        const authError = parseAuthError(err);
        setStatusCode(null);
        setError(authError);
        if (authError.code === "challenge_expired" || authError.code === "challenge_attempts_exhausted") {
          setStep("email");
          setChallenge(null);
          setPendingReturnUrl(undefined);
        }
      }
    },
    [challenge, completeMutation, navigate, pendingReturnUrl, queryClient, returnUrlSearchParams]
  );

  const resend = useCallback(async () => {
    if (!email) return;

    setError(null);
    setStatusCode(null);
    try {
      const finalReturnUrl =
        pendingReturnUrl ?? getRequestedReturnUrl(returnUrlSearchParams);
      const result = await beginMutation.mutateAsync({
        data: { email, returnUrl: finalReturnUrl ?? null },
      });
      setChallenge(result);
      setStatusCode("auth.otp.emailCodeResent");
      setStep("code");
    } catch (err) {
      setStatusCode(null);
      setError(parseAuthError(err));
    }
  }, [email, beginMutation, pendingReturnUrl, returnUrlSearchParams]);

  const reset = useCallback(() => {
    setStep("email");
    setChallenge(null);
    setError(null);
    setStatusCode(null);
    setEmail("");
    setPendingReturnUrl(undefined);
  }, []);

  return {
    step,
    challenge,
    email,
    begin,
    complete,
    resend,
    reset,
    isLoading: beginMutation.isPending || completeMutation.isPending,
    error,
    errorMessage: error ? getErrorMessage(error) : null,
    statusCode,
    statusMessage: null,
    remainingAttempts: challenge?.remainingAttempts,
    expiresAt: challenge?.expiresAt,
  };
}
