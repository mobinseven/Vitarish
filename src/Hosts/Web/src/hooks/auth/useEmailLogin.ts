import { useCallback, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  useBeginEmailLoginMutation,
  useCompleteEmailLoginMutation,
} from "@/api/generated/authentication/authentication";
import type { AuthenticationChallengeDto } from "@/api/generated/authentication/model";
import { parseAuthError, getErrorMessage, type AuthError } from "@/lib/errors";

export type EmailLoginStep = "email" | "code";

export function useEmailLogin() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const beginMutation = useBeginEmailLoginMutation();
  const completeMutation = useCompleteEmailLoginMutation();
  const [step, setStep] = useState<EmailLoginStep>("email");
  const [challenge, setChallenge] = useState<AuthenticationChallengeDto | null>(
    null
  );
  const [error, setError] = useState<AuthError | null>(null);
  const [email, setEmail] = useState("");

  const begin = useCallback(
    async (emailAddr: string, returnUrl?: string) => {
      setError(null);
      try {
        const finalReturnUrl = returnUrl ?? searchParams.get("returnUrl") ?? "/";
        const result = await beginMutation.mutateAsync({
          email: emailAddr,
          returnUrl: finalReturnUrl,
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
        const returnUrl = searchParams.get("returnUrl") ?? "/";
        const result = await completeMutation.mutateAsync({
          challengeId: challenge.challengeId,
          code,
          returnUrl,
        });
        router.push(result.returnUrl);
      } catch (err) {
        const authError = parseAuthError(err);
        setError(authError);
        if (authError.code === "challenge_expired" || authError.code === "challenge_attempts_exhausted") {
          setStep("email");
          setChallenge(null);
        }
      }
    },
    [challenge, completeMutation, router, searchParams]
  );

  const reset = useCallback(() => {
    setStep("email");
    setChallenge(null);
    setError(null);
    setEmail("");
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
