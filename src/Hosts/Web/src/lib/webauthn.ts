export interface PublicKeyCredentialRequestOptionsJSON {
  challenge: string;
  timeout?: number;
  rpId?: string;
  allowCredentials?: Array<{
    id: string;
    type: "public-key";
    transports?: AuthenticatorTransport[];
  }>;
  userVerification?: UserVerificationRequirement;
}

export class WebAuthnUnsupportedError extends Error {
  constructor(message = "WebAuthn is not supported in this browser or context.") {
    super(message);
    this.name = "WebAuthnUnsupportedError";
  }
}

export class WebAuthnOptionConversionError extends Error {
  constructor(message = "WebAuthn options could not be converted for the browser.") {
    super(message);
    this.name = "WebAuthnOptionConversionError";
  }
}

export class WebAuthnRegistrationError extends Error {
  constructor(message = "WebAuthn registration failed.") {
    super(message);
    this.name = "WebAuthnRegistrationError";
  }
}

export async function startRegistration(
  options: PublicKeyCredentialCreationOptionsJSON
): Promise<PublicKeyCredentialCreationOptionsJSON> {
  assertWebAuthnSupported("Passkey registration");

  const credentialOptions = toBrowserRegistrationOptions(options);

  let credential: PublicKeyCredential | null;
  try {
    credential = (await navigator.credentials.create({
      publicKey: credentialOptions,
    })) as PublicKeyCredential | null;
  } catch (error) {
    if (error instanceof DOMException && error.name === "NotAllowedError") {
      throw error;
    }

    throw new WebAuthnRegistrationError(
      error instanceof Error ? error.message : "Passkey registration failed."
    );
  }

  if (!credential) {
    throw new WebAuthnRegistrationError(
      "Passkey registration did not return a credential."
    );
  }

  const response = credential.response as AuthenticatorAttestationResponse;

  return {
    id: credential.id,
    rawId: bufferToBase64Url(credential.rawId),
    type: credential.type,
    response: {
      attestationObject: bufferToBase64Url(response.attestationObject),
      clientDataJSON: bufferToBase64Url(response.clientDataJSON),
    },
  } as unknown as PublicKeyCredentialCreationOptionsJSON;
}

export async function startPasskeyAuthentication(
  optionsJson: string
): Promise<string> {
  const options: PublicKeyCredentialRequestOptionsJSON = JSON.parse(optionsJson);

  const challenge = base64UrlToBuffer(options.challenge);

  const credentialOptions: PublicKeyCredentialRequestOptions = {
    challenge,
    timeout: options.timeout,
    rpId: options.rpId,
    allowCredentials: options.allowCredentials?.map((cred) => ({
      id: base64UrlToBuffer(cred.id),
      type: cred.type as PublicKeyCredentialType,
      transports: cred.transports,
    })),
    userVerification: options.userVerification,
  };

  assertWebAuthnSupported("Passkey authentication");

  const credential = (await navigator.credentials.get({
    publicKey: credentialOptions,
  })) as PublicKeyCredential | null;

  if (!credential) {
    throw new Error("Passkey authentication was cancelled");
  }

  const response = credential.response as AuthenticatorAssertionResponse;

  return JSON.stringify({
    id: credential.id,
    rawId: bufferToBase64Url(credential.rawId),
    type: credential.type,
    response: {
      authenticatorData: bufferToBase64Url(response.authenticatorData),
      clientDataJSON: bufferToBase64Url(response.clientDataJSON),
      signature: bufferToBase64Url(response.signature),
      userHandle: response.userHandle
        ? bufferToBase64Url(response.userHandle)
        : null,
    },
  });
}

function assertWebAuthnSupported(operation: string): void {
  if (
    typeof window === "undefined" ||
    window.PublicKeyCredential === undefined ||
    window.isSecureContext !== true ||
    navigator.credentials === undefined
  ) {
    throw new WebAuthnUnsupportedError(
      `${operation} is not supported in this browser or context (requires WebAuthn and HTTPS).`
    );
  }
}

function toBrowserRegistrationOptions(
  options: PublicKeyCredentialCreationOptionsJSON
): PublicKeyCredentialCreationOptions {
  try {
    return {
      ...options,
      challenge: base64UrlToBuffer(options.challenge),
      user: {
        ...options.user,
        id: base64UrlToBuffer(options.user.id as string),
      },
      excludeCredentials: options.excludeCredentials?.map((cred) => ({
        id: base64UrlToBuffer(cred.id as string),
        type: "public-key" as PublicKeyCredentialType,
        transports: cred.transports as AuthenticatorTransport[] | undefined,
      })),
    } as PublicKeyCredentialCreationOptions;
  } catch (error) {
    throw new WebAuthnOptionConversionError(
      error instanceof Error
        ? `Invalid WebAuthn registration options: ${error.message}`
        : "Invalid WebAuthn registration options."
    );
  }
}

function base64UrlToBuffer(base64Url: string): ArrayBuffer {
  const base64UrlPattern = /^[A-Za-z0-9_-]+$/;
  if (!base64UrlPattern.test(base64Url)) {
    throw new Error("Invalid base64url data.");
  }

  const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
  const padded = base64 + "=".repeat((4 - (base64.length % 4)) % 4);
  const binary = atob(padded);
  const buffer = new ArrayBuffer(binary.length);
  const view = new Uint8Array(buffer);
  for (let i = 0; i < binary.length; i++) {
    view[i] = binary.charCodeAt(i);
  }
  return buffer;
}

function bufferToBase64Url(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = "";
  for (let i = 0; i < bytes.length; i++) {
    binary += String.fromCharCode(bytes[i]);
  }
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}
