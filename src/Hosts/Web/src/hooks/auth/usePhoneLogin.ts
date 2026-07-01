import { useCallback, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  useAuthenticationLoginPhoneBegin,
  useAuthenticationLoginPhoneComplete,
} from "@/api/generated/authentication/authentication";
import type { AuthenticationChallengeDto, BeginPhoneLoginRequest, CompletePhoneLoginRequest } from "@/api/generated/authentication/model";
import { parseAuthError, getErrorMessage, type AuthError } from "@/lib/errors";
import { getRequestedReturnUrl } from "./requestedReturnUrl";

export type PhoneLoginStep = "phone" | "code";

export function usePhoneLogin() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const beginMutation = useAuthenticationLoginPhoneBegin();
  const completeMutation = useAuthenticationLoginPhoneComplete();
  const [step, setStep] = useState<PhoneLoginStep>("phone");
  const [challenge, setChallenge] = useState<AuthenticationChallengeDto | null>(
    null
  );
  const [error, setError] = useState<AuthError | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [phoneNumber, setPhoneNumber] = useState("");
  const [pendingReturnUrl, setPendingReturnUrl] = useState<string | undefined>(
    undefined
  );

  const begin = useCallback(
    async (phone: string, returnUrl?: string) => {
      setError(null);
      setStatusMessage(null);
      try {
        const finalReturnUrl = getRequestedReturnUrl(searchParams, returnUrl);
        setPendingReturnUrl(finalReturnUrl ?? undefined);
        const result = await beginMutation.mutateAsync({
          data: { phoneNumber: phone, ...(finalReturnUrl ? { returnUrl: finalReturnUrl } : {}) } as unknown as BeginPhoneLoginRequest,
        });
        setChallenge(result);
        setPhoneNumber(phone);
        setStep("code");
      } catch (err) {
        setStatusMessage(null);
        setError(parseAuthError(err));
      }
    },
    [beginMutation, searchParams]
  );

  const complete = useCallback(
    async (code: string) => {
      if (!challenge) return;
      setError(null);
      setStatusMessage(null);
      try {
        const returnUrl =
          pendingReturnUrl ?? getRequestedReturnUrl(searchParams);
        const result = await completeMutation.mutateAsync({
          data: { challengeId: challenge.challengeId, code, ...(returnUrl ? { returnUrl } : {}) } as unknown as CompletePhoneLoginRequest,
        });
        router.push(result.returnUrl);
      } catch (err) {
        const authError = parseAuthError(err);
        setStatusMessage(null);
        setError(authError);
        if (authError.code === "challenge_expired" || authError.code === "challenge_attempts_exhausted") {
          setStep("phone");
          setChallenge(null);
          setPendingReturnUrl(undefined);
        }
      }
    },
    [challenge, completeMutation, router, searchParams, pendingReturnUrl]
  );

  const resend = useCallback(async () => {
    if (!phoneNumber) return;

    setError(null);
    setStatusMessage(null);
    try {
      const finalReturnUrl =
        pendingReturnUrl ?? getRequestedReturnUrl(searchParams);
      const result = await beginMutation.mutateAsync({
        data: { phoneNumber, ...(finalReturnUrl ? { returnUrl: finalReturnUrl } : {}) } as unknown as BeginPhoneLoginRequest,
      });
      setChallenge(result);
      setStatusMessage("A new code was sent to your phone number.");
      setStep("code");
    } catch (err) {
      setStatusMessage(null);
      setError(parseAuthError(err));
    }
  }, [phoneNumber, beginMutation, pendingReturnUrl, searchParams]);

  const reset = useCallback(() => {
    setStep("phone");
    setChallenge(null);
    setError(null);
    setStatusMessage(null);
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
    statusMessage,
    remainingAttempts: challenge?.remainingAttempts,
    expiresAt: challenge?.expiresAt,
  };
}
