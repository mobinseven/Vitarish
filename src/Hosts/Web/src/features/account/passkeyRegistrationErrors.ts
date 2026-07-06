import { ApiError } from "@/api/client/apiFetch";
import { localizeApiError } from "@/localization";
import type { Translate } from "@/localization/messageTypes";
import { PasskeyRegistrationOptionsError } from "./hooks/passkeyRegistrationOptions";
import {
  WebAuthnOptionConversionError,
  WebAuthnRegistrationError,
  WebAuthnUnsupportedError,
} from "@/lib/webauthn";

export function localizePasskeyRegistrationError(
  err: unknown,
  t: Translate
): string {
  if (err instanceof ApiError) {
    return localizeApiError(err, t);
  }

  if (hasErrorName(err, "NotAllowedError")) {
    return t("account.passkeys.registrationCancelled");
  }

  if (err instanceof WebAuthnUnsupportedError) {
    return t("account.passkeys.unsupported");
  }

  if (
    err instanceof PasskeyRegistrationOptionsError ||
    err instanceof WebAuthnOptionConversionError ||
    err instanceof WebAuthnRegistrationError
  ) {
    return t("account.passkeys.registerFailed");
  }

  return t("account.passkeys.registerFailed");
}

function hasErrorName(err: unknown, name: string): boolean {
  return (
    typeof err === "object" &&
    err !== null &&
    "name" in err &&
    (err as { name?: unknown }).name === name
  );
}
