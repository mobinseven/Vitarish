export class PasskeyRegistrationOptionsError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "PasskeyRegistrationOptionsError";
  }
}

export function parsePasskeyRegistrationOptions(
  optionsJson: unknown
): PublicKeyCredentialCreationOptionsJSON {
  if (typeof optionsJson !== "string" || optionsJson.trim().length === 0) {
    throw new PasskeyRegistrationOptionsError(
      "Passkey registration options response must be a non-empty JSON string."
    );
  }

  let options: unknown;
  try {
    options = JSON.parse(optionsJson);
  } catch {
    throw new PasskeyRegistrationOptionsError(
      "Passkey registration options response was not valid JSON."
    );
  }

  assertPasskeyRegistrationOptions(options);
  return options;
}

function assertPasskeyRegistrationOptions(
  options: unknown
): asserts options is PublicKeyCredentialCreationOptionsJSON {
  if (typeof options !== "object" || options === null) {
    throw new PasskeyRegistrationOptionsError(
      "Passkey registration options response must be an object."
    );
  }

  const record = options as Record<string, unknown>;
  assertNonEmptyString(record.challenge, "challenge");
  assertRelyingParty(record.rp);
  assertUser(record.user);
  assertPubKeyCredParams(record.pubKeyCredParams);
}

function assertRelyingParty(rp: unknown): void {
  if (typeof rp !== "object" || rp === null) {
    throw new PasskeyRegistrationOptionsError(
      "Passkey registration options must include relying party details."
    );
  }

  assertNonEmptyString((rp as Record<string, unknown>).name, "rp.name");
}

function assertUser(user: unknown): void {
  if (typeof user !== "object" || user === null) {
    throw new PasskeyRegistrationOptionsError(
      "Passkey registration options must include user details."
    );
  }

  const record = user as Record<string, unknown>;
  assertNonEmptyString(record.id, "user.id");
  assertNonEmptyString(record.name, "user.name");
  assertNonEmptyString(record.displayName, "user.displayName");
}

function assertPubKeyCredParams(pubKeyCredParams: unknown): void {
  if (!Array.isArray(pubKeyCredParams) || pubKeyCredParams.length === 0) {
    throw new PasskeyRegistrationOptionsError(
      "Passkey registration options must include credential parameters."
    );
  }
}

function assertNonEmptyString(value: unknown, fieldName: string): void {
  if (typeof value !== "string" || value.trim().length === 0) {
    throw new PasskeyRegistrationOptionsError(
      `Passkey registration options must include ${fieldName}.`
    );
  }
}
