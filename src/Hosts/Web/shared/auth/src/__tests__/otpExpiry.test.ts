import { describe, expect, it } from "vitest";
import { getOtpExpiryPresentation } from "../auth/otpExpiry";

describe("getOtpExpiryPresentation", () => {
  it("returns a short countdown", () => {
    const now = new Date("2026-07-01T10:00:00.000Z");
    const result = getOtpExpiryPresentation("2026-07-01T10:04:32.000Z", now);

    expect(result).toEqual({
      remainingSeconds: 272,
      isExpired: false,
      remaining: "04:32",
    });
  });

  it("reports expiry without a countdown", () => {
    const now = new Date("2026-07-01T10:00:00.000Z");
    const result = getOtpExpiryPresentation("2026-07-01T09:59:59.000Z", now);

    expect(result.isExpired).toBe(true);
    expect(result.remaining).toBeNull();
  });
});
