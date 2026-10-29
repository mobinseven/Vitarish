"use client";

import { useCallback, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  useAuthenticationLoginPhoneBegin,
  useAuthenticationLoginPhoneComplete,
  getAuthenticationSessionGetQueryKey,
} from "../api/generated/authentication/authentication";
import type { AuthenticationChallengeDto } from "../api/generated/authentication/model";
import { parseAuthError, getErrorMessage, type AuthError } from "../auth/errors";
import { getRequestedReturnUrl } from "./requestedReturnUrl";
import { ignoreAuthNavigation, type AuthNavigationOptions } from "./navigation";

export type PhoneLoginStep = "phone" | "code";

export function usePhoneLogin({ navigate = ignoreAuthNavigation, returnUrlSearchParams }: AuthNavigationOptions = {}) {
  const beginMutation = useAuthenticationLoginPhoneBegin();
  const completeMutation = useAuthenticationLoginPhoneComplete();
  const queryClient = useQueryClient();
  const [step, setStep] = useState<PhoneLoginStep>("phone");
  const [challenge, setChallenge] = useState<AuthenticationChallengeDto | null>(
    null
  );
  const [error, setError] = useState<AuthError | null>(null);
  const [statusCode, setStatusCode] = useState<string | null>(null);
  const [phoneNumber, setPhoneNumber] = useState("");
  const [pendingReturnUrl, setPendingReturnUrl] = useState<string | undefined>(
    undefined
  );

  const begin = useCallback(
    async (phone: string, returnUrl?: string) => {
      setError(null);
      setStatusCode(null);
      try {
        const finalReturnUrl = getRequestedReturnUrl(returnUrlSearchParams, returnUrl);
        setPendingReturnUrl(finalReturnUrl ?? undefined);
        const result = await beginMutation.mutateAsync({
          data: { phoneNumber: phone, returnUrl: finalReturnUrl ?? null },
        });
        setChallenge(result);
        setPhoneNumber(phone);
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
          setStep("phone");
          setChallenge(null);
          setPendingReturnUrl(undefined);
        }
      }
    },
    [challenge, completeMutation, navigate, pendingReturnUrl, queryClient, returnUrlSearchParams]
  );

  const resend = useCallback(async () => {
    if (!phoneNumber) return;

    setError(null);
    setStatusCode(null);
    try {
      const finalReturnUrl =
        pendingReturnUrl ?? getRequestedReturnUrl(returnUrlSearchParams);
      const result = await beginMutation.mutateAsync({
        data: { phoneNumber, returnUrl: finalReturnUrl ?? null },
      });
      setChallenge(result);
      setStatusCode("auth.otp.phoneCodeResent");
      setStep("code");
    } catch (err) {
      setStatusCode(null);
      setError(parseAuthError(err));
    }
  }, [phoneNumber, beginMutation, pendingReturnUrl, returnUrlSearchParams]);

  const reset = useCallback(() => {
    setStep("phone");
    setChallenge(null);
    setError(null);
    setStatusCode(null);
    setPhoneNumber("");
    setPendingReturnUrl(undefined);
  }, []);

  return {
    step,
    challenge,
    phoneNumber,
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
