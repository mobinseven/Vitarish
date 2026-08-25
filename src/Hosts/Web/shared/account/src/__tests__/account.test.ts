import { describe, expect, it } from "vitest";
import { useAccountPasskeys } from "../hooks/useAccountPasskeys";
import { localizePasskeyRegistrationError } from "../passkeyRegistrationErrors";

describe("account exports", () => {
  it("exports key hooks", () => {
    expect(typeof useAccountPasskeys).toBe("function");
  });

  it("exports localizePasskeyRegistrationError", () => {
    expect(typeof localizePasskeyRegistrationError).toBe("function");
  });
});
