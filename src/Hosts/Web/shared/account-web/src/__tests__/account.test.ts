import { describe, expect, it } from "vitest";
import { useAccountTheme } from "../hooks/useAccountTheme";
import { useAccountPasskeys } from "../hooks/useAccountPasskeys";
import { localizePasskeyRegistrationError } from "../passkeyRegistrationErrors";

describe("account-web exports", () => {
  it("exports key hooks", () => {
    expect(typeof useAccountTheme).toBe("function");
    expect(typeof useAccountPasskeys).toBe("function");
  });

  it("exports localizePasskeyRegistrationError", () => {
    expect(typeof localizePasskeyRegistrationError).toBe("function");
  });
});
