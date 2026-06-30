import { useCallback, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  useAuthenticationLoginEmailBegin,
  useAuthenticationLoginEmailComplete,
} from "@/api/generated/authentication/authentication";
import type { AuthenticationChallengeDto } from "@/api/generated/authentication/model";
import { parseAuthError, getErrorMessage, type AuthError } from "@/lib/errors";
import { getRequestedReturnUrl } from "./requestedReturnUrl";

export type EmailLoginStep = "email" | "code";

export function useEmailLogin() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const beginMutation = useAuthenticationLoginEmailBegin();
  const completeMutation = useAuthenticationLoginEmailComplete();
  const [step, setStep] = useState<EmailLoginStep>("email");
  const [challenge, setChallenge] = useState<AuthenticationChallengeDto | null>(
    null
  );
  const [error, setError] = useState<AuthError | null>(null);
  const [email, setEmail] = useState("");
  const [pendingReturnUrl, setPendingReturnUrl] = useState<string | undefined>(
    undefined
  );

  const begin = useCallback(
    async (emailAddr: string, returnUrl?: string) => {
      setError(null);
      try {
        const finalReturnUrl = getRequestedReturnUrl(searchParams, returnUrl);
        setPendingReturnUrl(finalReturnUrl ?? undefined);
        const result = await beginMutation.mutateAsync({
          data: { email: emailAddr, returnUrl: finalReturnUrl },
        });
        setChallenge(result);
        setEmail(emailAddr);
        setStep("code");
      } catch (err) {
        setError(parseAuthError(err));
      }
    },
    [beginMutation, searchParams]
  );

  const complete = useCallback(
    async (code: string) => {
      if (!challenge) return;
      setError(null);
      try {
        const returnUrl =
          pendingReturnUrl ?? getRequestedReturnUrl(searchParams);
        const result = await completeMutation.mutateAsync({
          data: { challengeId: challenge.challengeId, code, returnUrl },
        });
        router.push(result.returnUrl);
      } catch (err) {
        const authError = parseAuthError(err);
        setError(authError);
        if (authError.code === "challenge_expired" || authError.code === "challenge_attempts_exhausted") {
          setStep("email");
          setChallenge(null);
          setPendingReturnUrl(undefined);
        }
      }
    },
    [challenge, completeMutation, router, searchParams, pendingReturnUrl]
  );

  const reset = useCallback(() => {
    setStep("email");
    setChallenge(null);
    setError(null);
    setEmail("");
    setPendingReturnUrl(undefined);
  }, []);

  return {
    step,
    challenge,
    email,
    begin,
    complete,
    reset,
    isLoading: beginMutation.isPending || completeMutation.isPending,
    error,
    errorMessage: error ? getErrorMessage(error) : null,
    remainingAttempts: challenge?.remainingAttempts,
    expiresAt: challenge?.expiresAt,
  };
}
