"use client";

import { useLocalizedText } from "@vitarish/localization";
import type { AuthError, AuthenticationErrorCode } from "../auth/errors";

const localizedFailureKeys: Partial<Record<AuthenticationErrorCode, string>> = {
  invalid_credentials: "auth.failures.invalidCredentials",
  locked_out: "auth.failures.lockedOut",
  challenge_expired: "auth.failures.challengeExpired",
  challenge_attempts_exhausted: "auth.failures.challengeAttemptsExhausted",
  invalid_verification_code: "auth.failures.invalidVerificationCode",
  too_many_attempts: "auth.failures.tooManyAttempts",
  provider_failure: "auth.failures.providerFailure",
  not_allowed: "auth.failures.notAllowed",
  unauthorized: "auth.failures.unauthorized",
  unknown_credential: "auth.failures.unknownCredential",
  invalid_credential: "auth.failures.invalidCredential",
  logout_context_unavailable: "auth.failures.logoutContextUnavailable",
  account_disabled: "auth.accountStatus.disabled",
  no_access: "auth.accountStatus.noAccess",
};

/**
 * Resolves the localized message a sign-in surface shows for an authentication
 * error. Codes without a catalog entry keep the transport message.
 */
export function useAuthErrorDisplay(
  error: AuthError | null,
  fallbackMessage: string | null,
): string | null {
  const { t } = useLocalizedText();
  const key = error ? localizedFailureKeys[error.code] : undefined;
  return key ? t(key) : fallbackMessage;
}
