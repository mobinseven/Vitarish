import { useState, useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useUsersGetProfile } from "@vitarish/auth";
import { apiFetch } from "@vitarish/api-client";
import { accountKeys } from "./accountKeys";

interface PhoneChangeResult {
  challengeId: string;
}

interface PhoneVerifyResult {
  succeeded: boolean;
}

export function useAccountPhoneChange(userId: string) {
  const queryClient = useQueryClient();
  const { data: profile } = useUsersGetProfile(userId);
  const [step, setStep] = useState<"phone" | "code">("phone");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [code, setCode] = useState("");
  const [challengeId, setChallengeId] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const reset = useCallback(() => {
    setStep("phone");
    setPhoneNumber("");
    setCode("");
    setChallengeId("");
    setError(null);
    setSuccess(false);
  }, []);

  const beginPhoneChange = useCallback(
    async (newPhone: string) => {
      setError(null);
      setSuccess(false);
      setIsLoading(true);
      try {
        const result = await apiFetch<PhoneChangeResult>(
          "/api/modules/authentication/account/phone/begin",
          {
            method: "POST",
            body: JSON.stringify({ phoneNumber: newPhone }),
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

  const verifyPhoneChange = useCallback(
    async (otpCode: string) => {
      setError(null);
      setSuccess(false);
      setIsLoading(true);
      try {
        await apiFetch<PhoneVerifyResult>(
          "/api/modules/authentication/account/phone/complete",
          {
            method: "POST",
            body: JSON.stringify({ challengeId, code: otpCode }),
          }
        );
        setSuccess(true);
        setStep("phone");
        setPhoneNumber("");
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
    phoneNumber,
    code,
    challengeId,
    isLoading,
    error,
    success,
    setPhoneNumber,
    setCode,
    setError,
    setSuccess,
    reset,
    beginPhoneChange,
    verifyPhoneChange,
  };
}
