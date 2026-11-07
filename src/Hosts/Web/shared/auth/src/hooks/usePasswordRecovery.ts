"use client";

import { useCallback, useState } from "react";
import { usePendingAuthenticationChallenge } from "./usePendingAuthenticationChallenge";
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

  const pending = usePendingAuthenticationChallenge(value => {
    if (value.isPasswordRecovery) setChallengeId(value.challenge.challengeId);
  }, () => setErrorMessage(t("auth.recovery.requestFailed")));

  const begin = useCallback(async (channel: PasswordRecoveryChannel, contact: string) => {
    pending.cancelRestore();
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
      pending.clear();
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
    pending.clear();
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
    isLoading: pending.isRestoring || beginMutation.isPending || completeMutation.isPending,
  };
}
