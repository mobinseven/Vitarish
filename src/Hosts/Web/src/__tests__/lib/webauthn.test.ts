import { describe, it, expect } from "vitest";

describe("webauthn helpers", () => {
  it("should export startPasskeyAuthentication function", async () => {
    const mod = await import("@/lib/webauthn");
    expect(typeof mod.startPasskeyAuthentication).toBe("function");
  });
});
