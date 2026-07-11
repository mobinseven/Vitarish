import { describe, expect, it } from "vitest";
import { ApiError } from "@vitarish/web-api-client";
import { localizePasskeyRegistrationError } from "../passkeyRegistrationErrors";
import { PasskeyRegistrationOptionsError } from "../hooks/passkeyRegistrationOptions";
import { PasskeyRegistrationStageError } from "../hooks/passkeyRegistrationStageError";
import { WebAuthnRegistrationError, WebAuthnUnsupportedError } from "../auth/webauthn";

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

  it("maps invalid begin-register options to generic passkey-specific failure", () => {
    expect(localizePasskeyRegistrationError(new PasskeyRegistrationOptionsError("bad options"), t)).toBe(
      "account.passkeys.registerFailed"
    );
  });

  it.each([
    ["begin", "account.passkeys.registerBeginFailed"],
    ["parseOptions", "account.passkeys.registerOptionsFailed"],
    ["browserCreate", "account.passkeys.registerBrowserCreateFailed"],
    ["complete", "account.passkeys.registerCompleteFailed"],
  ] as const)("maps %s stage failures to localized stage keys", (stage, key) => {
    expect(localizePasskeyRegistrationError(new PasskeyRegistrationStageError(stage, new Error("failed")), t)).toBe(
      key
    );
  });

  it("keeps cancelled browser creation distinct when the stage error cause is NotAllowedError", () => {
    expect(
      localizePasskeyRegistrationError(
        new PasskeyRegistrationStageError("browserCreate", new DOMException("cancelled", "NotAllowedError")),
        t
      )
    ).toBe("account.passkeys.registrationCancelled");
  });

  it("maps wrapped ApiError to localized API error", () => {
    expect(
      localizePasskeyRegistrationError(
        new PasskeyRegistrationStageError("begin", new ApiError({ status: 401, code: "Unauthorized" })),
        t
      )
    ).toBe("errors.unauthorized");
  });

  it("maps wrapped WebAuthnUnsupportedError to unsupported message", () => {
    expect(
      localizePasskeyRegistrationError(
        new PasskeyRegistrationStageError("browserCreate", new WebAuthnUnsupportedError()),
        t
      )
    ).toBe("account.passkeys.unsupported");
  });

  it("maps unknown WebAuthn registration failures to passkey-specific failure", () => {
    expect(localizePasskeyRegistrationError(new WebAuthnRegistrationError("browser failed"), t)).toBe(
      "account.passkeys.registerFailed"
    );
  });
});
