"use client";

import { useLocalizedText } from "@vitarish/localization";
import type { AuthError } from "../auth/errors";

/**
 * Resolves the message a sign-in surface shows for an authentication error.
 * Account-state denials use the shared accountStatus catalog so the message is
 * localized; every other error keeps the existing message.
 */
export function useAuthErrorDisplay(
  error: AuthError | null,
  fallbackMessage: string | null,
): string | null {
  const { t } = useLocalizedText();

  if (error?.code === "account_disabled") {
    return t("auth.accountStatus.disabled");
  }
  if (error?.code === "no_access") {
    return t("auth.accountStatus.noAccess");
  }
  return fallbackMessage;
}
