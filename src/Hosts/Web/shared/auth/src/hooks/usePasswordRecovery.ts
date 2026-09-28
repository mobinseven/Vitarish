"use client";

import { useCallback, useState } from "react";
import { useLocalizedText } from "@vitarish/localization";
import {
  useAuthenticationPasswordRecoveryBegin,
  useAuthenticationPasswordRecoveryComplete,
} from "../api/generated/authentication/authentication";
import type { PasswordRecoveryChannel } from "../auth/passwordRecoveryChannel";
import { passwordRecoveryChannel } from "../auth/passwordRecoveryChannel";
import { parseAuthError } from "../auth/errors";

export function usePasswordRecovery() {
  const beginMutation = useAuthenticationPasswordRecoveryBegin();
  const completeMutation = useAuthenticationPasswordRecoveryComplete();
  const { t } = useLocalizedText();
  const [challengeId, setChallengeId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isComplete, setIsComplete] = useState(false);

  const begin = useCallback(async (channel: PasswordRecoveryChannel, contact: string) => {
    setErrorMessage(null);
    try {
      const result = await beginMutation.mutateAsync({
        data: { channel: passwordRecoveryChannel[channel], contact },
      });
      setChallengeId(result.challengeId);
    } catch (error) {
      const authError = parseAuthError(error);
      setErrorMessage(authError.code === "too_many_attempts" || authError.code === "locked_out"
        ? t("auth.recovery.tooManyAttempts")
        : t("auth.recovery.requestFailed"));
    }
  }, [beginMutation, t]);

  const complete = useCallback(async (code: string, newPassword: string) => {
    if (!challengeId) return;
    setErrorMessage(null);
    try {
      await completeMutation.mutateAsync({
        data: { challengeId, code, newPassword },
      });
      setIsComplete(true);
    } catch (error) {
      const authError = parseAuthError(error);
      setErrorMessage(authError.code === "invalid_input"
        ? t("auth.recovery.passwordPolicyError")
        : authError.code === "challenge_expired"
          ? t("auth.recovery.codeExpired")
          : authError.code === "challenge_attempts_exhausted" || authError.code === "too_many_attempts" || authError.code === "locked_out"
            ? t("auth.recovery.tooManyAttempts")
            : authError.code === "invalid_verification_code" || authError.code === "challenge_not_found" || authError.code === "challenge_already_used"
              ? t("auth.recovery.invalidCode")
              : t("auth.recovery.requestFailed"));
    }
  }, [challengeId, completeMutation, t]);

  const startOver = useCallback(() => {
    setChallengeId(null);
    setErrorMessage(null);
    setIsComplete(false);
  }, []);

  return {
    begin,
    complete,
    startOver,
    errorMessage,
    hasChallenge: challengeId !== null,
    isComplete,
    isLoading: beginMutation.isPending || completeMutation.isPending,
  };
}
