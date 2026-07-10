import { afterEach, describe, expect, it, vi } from "vitest";
import {
  startRegistration,
  WebAuthnRegistrationError,
  WebAuthnUnsupportedError,
} from "@/lib/webauthn";

const validOptions = {
  challenge: "Y2hhbGxlbmdl",
  rp: { name: "Vitarish" },
  user: { id: "dXNlcg", name: "user@example.com", displayName: "User" },
  pubKeyCredParams: [{ type: "public-key", alg: -7 }],
};

describe("webauthn helpers", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("should export startPasskeyAuthentication function", async () => {
    const mod = await import("@/lib/webauthn");
    expect(typeof mod.startPasskeyAuthentication).toBe("function");
  });

  it("throws NotAllowedError when browser cancels registration", async () => {
    Object.defineProperty(window, "PublicKeyCredential", {
      value: function PublicKeyCredential() {},
      configurable: true,
    });
    Object.defineProperty(window, "isSecureContext", {
      value: true,
      configurable: true,
    });
    Object.defineProperty(navigator, "credentials", {
      value: {
        create: vi
          .fn()
          .mockRejectedValue(new DOMException("cancelled", "NotAllowedError")),
      },
      configurable: true,
    });

    await expect(
      startRegistration(validOptions as PublicKeyCredentialCreationOptionsJSON)
    ).rejects.toMatchObject({
      name: "NotAllowedError",
    });
  });

  it("throws unsupported error when WebAuthn browser context is unavailable", async () => {
    Object.defineProperty(window, "PublicKeyCredential", {
      value: undefined,
      configurable: true,
    });
    Object.defineProperty(window, "isSecureContext", {
      value: false,
      configurable: true,
    });
    Object.defineProperty(navigator, "credentials", {
      value: undefined,
      configurable: true,
    });

    await expect(
      startRegistration(validOptions as PublicKeyCredentialCreationOptionsJSON)
    ).rejects.toBeInstanceOf(
      WebAuthnUnsupportedError
    );
  });

  it("throws registration error when navigator.credentials.create returns null", async () => {
    Object.defineProperty(window, "PublicKeyCredential", {
      value: function PublicKeyCredential() {},
      configurable: true,
    });
    Object.defineProperty(window, "isSecureContext", {
      value: true,
      configurable: true,
    });
    Object.defineProperty(navigator, "credentials", {
      value: { create: vi.fn().mockResolvedValue(null) },
      configurable: true,
    });

    await expect(
      startRegistration(validOptions as PublicKeyCredentialCreationOptionsJSON)
    ).rejects.toBeInstanceOf(
      WebAuthnRegistrationError
    );
  });
});
