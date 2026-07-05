import { useState, useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useUsersGetProfile } from "@/api/generated/users/users";
import { apiFetch } from "@/api/client/apiFetch";
import { accountKeys } from "./accountKeys";

interface EmailChangeResult {
  challengeId: string;
}

interface EmailVerifyResult {
  succeeded: boolean;
}

export function useAccountEmailChange(userId: string) {
  const queryClient = useQueryClient();
  const { data: profile } = useUsersGetProfile(userId);
  const [step, setStep] = useState<"email" | "code">("email");
  const [email, setEmail] = useState("");
  const [code, setCode] = useState("");
  const [challengeId, setChallengeId] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const reset = useCallback(() => {
    setStep("email");
    setEmail("");
    setCode("");
    setChallengeId("");
    setError(null);
    setSuccess(false);
  }, []);

  const beginEmailChange = useCallback(
    async (newEmail: string) => {
      setError(null);
      setSuccess(false);
      setIsLoading(true);
      try {
        const result = await apiFetch<EmailChangeResult>(
          "/api/modules/authentication/account/email/begin",
          {
            method: "POST",
            body: JSON.stringify({ email: newEmail }),
          }
        );
        setChallengeId(result.challengeId);
        setStep("code");
      } catch (err: unknown) {
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  const verifyEmailChange = useCallback(
    async (otpCode: string) => {
      setError(null);
      setSuccess(false);
      setIsLoading(true);
      try {
        await apiFetch<EmailVerifyResult>(
          "/api/modules/authentication/account/email/complete",
          {
            method: "POST",
            body: JSON.stringify({ challengeId, code: otpCode }),
          }
        );
        setSuccess(true);
        setStep("email");
        setEmail("");
        setCode("");
        await queryClient.invalidateQueries({
          queryKey: accountKeys.profile(userId),
        });
      } catch (err: unknown) {
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [challengeId, queryClient, userId]
  );

  return {
    profile,
    step,
    email,
    code,
    challengeId,
    isLoading,
    error,
    success,
    setEmail,
    setCode,
    setError,
    setSuccess,
    reset,
    beginEmailChange,
    verifyEmailChange,
  };
}
