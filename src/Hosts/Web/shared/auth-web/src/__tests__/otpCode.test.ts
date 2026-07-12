import { describe, expect, it } from "vitest";
import { normalizeOtpCode } from "../auth/otpCode";

describe("normalizeOtpCode", () => {
  it.each([
    ["123456", "123456"],
    ["۱۲۳۴۵۶", "123456"],
    ["١٢٣٤٥٦", "123456"],
    ["a1 ۲-٣b4", "1234"],
    ["123456789", "123456"],
    ["abc!@#", ""],
  ])("normalizes %s to %s", (input, expected) => {
    expect(normalizeOtpCode(input)).toBe(expected);
  });

  it("uses the provided maximum length", () => {
    expect(normalizeOtpCode("123456", 4)).toBe("1234");
  });
});
