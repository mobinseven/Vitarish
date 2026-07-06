import { describe, expect, it } from "vitest";
import {
  parsePasskeyRegistrationOptions,
  PasskeyRegistrationOptionsError,
} from "@/features/account/hooks/passkeyRegistrationOptions";

const validOptions = {
  challenge: "Y2hhbGxlbmdl",
  rp: { name: "Vitarish" },
  user: { id: "dXNlcg", name: "user@example.com", displayName: "User" },
  pubKeyCredParams: [{ type: "public-key", alg: -7 }],
};

describe("parsePasskeyRegistrationOptions", () => {
  it("rejects malformed begin-register JSON", () => {
    expect(() => parsePasskeyRegistrationOptions("{")).toThrow(
      PasskeyRegistrationOptionsError
    );
  });

  it("rejects missing required WebAuthn option fields", () => {
    const missingChallenge = { ...validOptions, challenge: "" };

    expect(() => parsePasskeyRegistrationOptions(JSON.stringify(missingChallenge))).toThrow(
      PasskeyRegistrationOptionsError
    );
  });

  it("accepts required WebAuthn creation options", () => {
    expect(parsePasskeyRegistrationOptions(JSON.stringify(validOptions))).toEqual(validOptions);
  });
});
