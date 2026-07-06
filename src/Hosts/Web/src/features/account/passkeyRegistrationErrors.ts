import { ApiError } from "@/api/client/apiFetch";
import { localizeApiError } from "@/localization";
import type { Translate } from "@/localization/messageTypes";
import { PasskeyRegistrationOptionsError } from "./hooks/passkeyRegistrationOptions";
import {
  PasskeyRegistrationStageError,
  type PasskeyRegistrationStage,
} from "./hooks/passkeyRegistrationStageError";
import {
  WebAuthnOptionConversionError,
  WebAuthnRegistrationError,
  WebAuthnUnsupportedError,
} from "@/lib/webauthn";

const passkeyRegistrationStageKeys: Record<PasskeyRegistrationStage, string> = {
  begin: "account.passkeys.registerBeginFailed",
  parseOptions: "account.passkeys.registerOptionsFailed",
  browserCreate: "account.passkeys.registerBrowserCreateFailed",
  complete: "account.passkeys.registerCompleteFailed",
};

export function localizePasskeyRegistrationError(
  err: unknown,
  t: Translate
): string {
  const underlying =
    err instanceof PasskeyRegistrationStageError && err.cause
      ? err.cause
      : err;

  if (hasErrorName(underlying, "NotAllowedError")) {
    return t("account.passkeys.registrationCancelled");
  }

  if (underlying instanceof ApiError) {
    return localizeApiError(underlying, t);
  }

  if (underlying instanceof WebAuthnUnsupportedError) {
    return t("account.passkeys.unsupported");
  }

  if (err instanceof PasskeyRegistrationStageError) {
    return t(passkeyRegistrationStageKeys[err.stage]);
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
