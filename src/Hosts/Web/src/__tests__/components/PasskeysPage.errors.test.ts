import { describe, expect, it } from "vitest";
import { ApiError } from "@/api/client/apiFetch";
import { localizePasskeyRegistrationError } from "@/features/account/passkeyRegistrationErrors";
import { PasskeyRegistrationOptionsError } from "@/features/account/hooks/passkeyRegistrationOptions";
import { WebAuthnRegistrationError, WebAuthnUnsupportedError } from "@/lib/webauthn";

const t = (key: string) => key;

describe("localizePasskeyRegistrationError", () => {
  it("keeps API errors on the API error mapper", () => {
    expect(localizePasskeyRegistrationError(new ApiError({ status: 401, code: "Unauthorized" }), t)).toBe(
      "errors.unauthorized"
    );
  });

  it("maps NotAllowedError to registration cancellation", () => {
    expect(localizePasskeyRegistrationError(new DOMException("cancelled", "NotAllowedError"), t)).toBe(
      "account.passkeys.registrationCancelled"
    );
  });

  it("maps unsupported WebAuthn browser context", () => {
    expect(localizePasskeyRegistrationError(new WebAuthnUnsupportedError(), t)).toBe(
      "account.passkeys.unsupported"
    );
  });

  it("maps invalid begin-register options to passkey-specific failure", () => {
    expect(localizePasskeyRegistrationError(new PasskeyRegistrationOptionsError("bad options"), t)).toBe(
      "account.passkeys.registerFailed"
    );
  });

  it("maps unknown WebAuthn registration failures to passkey-specific failure", () => {
    expect(localizePasskeyRegistrationError(new WebAuthnRegistrationError("browser failed"), t)).toBe(
      "account.passkeys.registerFailed"
    );
  });
});
