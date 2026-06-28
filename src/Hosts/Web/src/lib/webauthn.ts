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

function base64UrlToBuffer(base64Url: string): ArrayBuffer {
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
