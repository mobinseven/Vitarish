import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import {
  useBeginPhoneLoginMutation,
  useCompletePhoneLoginMutation,
} from "@/api/generated/authentication/authentication";
import type { AuthenticationChallengeDto } from "@/api/generated/authentication/model";
import { parseAuthError, getErrorMessage, type AuthError } from "@/lib/errors";

export type PhoneLoginStep = "phone" | "code";

export function usePhoneLogin() {
  const router = useRouter();
  const beginMutation = useBeginPhoneLoginMutation();
  const completeMutation = useCompletePhoneLoginMutation();
  const [step, setStep] = useState<PhoneLoginStep>("phone");
  const [challenge, setChallenge] = useState<AuthenticationChallengeDto | null>(
    null
  );
  const [error, setError] = useState<AuthError | null>(null);
  const [phoneNumber, setPhoneNumber] = useState("");

  const begin = useCallback(
    async (phone: string, returnUrl?: string) => {
      setError(null);
      try {
        const result = await beginMutation.mutateAsync({
          phoneNumber: phone,
          returnUrl: returnUrl ?? "/",
        });
        setChallenge(result);
        setPhoneNumber(phone);
        setStep("code");
      } catch (err) {
        setError(parseAuthError(err));
      }
    },
    [beginMutation]
  );

  const complete = useCallback(
    async (code: string) => {
      if (!challenge) return;
      setError(null);
      try {
        const result = await completeMutation.mutateAsync({
          challengeId: challenge.challengeId,
          code,
          returnUrl: "/",
        });
        router.push(result.returnUrl);
      } catch (err) {
        const authError = parseAuthError(err);
        setError(authError);
        if (authError.code === "challenge_expired" || authError.code === "challenge_attempts_exhausted") {
          setStep("phone");
          setChallenge(null);
        }
      }
    },
    [challenge, completeMutation, router]
  );

  const reset = useCallback(() => {
    setStep("phone");
    setChallenge(null);
    setError(null);
    setPhoneNumber("");
  }, []);

  return {
    step,
    challenge,
    phoneNumber,
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
