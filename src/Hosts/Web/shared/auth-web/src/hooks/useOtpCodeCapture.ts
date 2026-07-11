import { useEffect } from "react";
import { normalizeOtpCode } from "../auth/otpCode";

interface OtpCredential extends Credential {
  code?: string;
}

interface OtpCredentialsContainer extends CredentialsContainer {
  get(options?: CredentialRequestOptions & {
    otp?: { transport: string[] };
    signal?: AbortSignal;
  }): Promise<OtpCredential | null>;
}

interface UseOtpCodeCaptureOptions {
  enabled: boolean;
  expectedLength?: number;
  onCode: (code: string) => void;
}

function getOtpCredentials(): OtpCredentialsContainer | null {
  if (typeof window === "undefined" || !("OTPCredential" in window)) {
    return null;
  }

  return navigator.credentials as OtpCredentialsContainer | undefined ?? null;
}

export function useOtpCodeCapture({
  enabled,
  expectedLength = 6,
  onCode,
}: UseOtpCodeCaptureOptions) {
  useEffect(() => {
    if (!enabled) {
      return undefined;
    }

    const credentials = getOtpCredentials();

    if (!credentials) {
      return undefined;
    }

    const abortController = new AbortController();

    credentials
      .get({
        otp: { transport: ["sms"] },
        signal: abortController.signal,
      })
      .then((credential) => {
        const normalizedCode = normalizeOtpCode(
          credential?.code ?? "",
          expectedLength
        );

        if (normalizedCode.length === expectedLength) {
          onCode(normalizedCode);
        }
      })
      .catch(() => {
        // Browser OTP capture can be unavailable, denied, or aborted. Manual entry remains available.
      });

    return () => abortController.abort();
  }, [enabled, expectedLength, onCode]);
}
