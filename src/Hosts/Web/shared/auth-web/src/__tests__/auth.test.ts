import { describe, expect, it } from "vitest";
import { ApiError } from "@vitarish/web-api-client";
import { parseAuthError, getErrorMessage } from "../auth/errors";
import { normalizeOtpCode } from "../auth/otpCode";
import { getOtpExpiryPresentation } from "../auth/otpExpiry";
import { getRequestedReturnUrl } from "../hooks/requestedReturnUrl";

describe("parseAuthError", () => {
  it("parses ApiError with code and status", () => {
    const error = new ApiError({ code: "invalid_credentials", detail: "Bad creds", status: 401 });
    const result = parseAuthError(error);
    expect(result.code).toBe("invalid_credentials");
    expect(result.status).toBe(401);
  });

  it("parses generic Error", () => {
    const result = parseAuthError(new Error("something broke"));
    expect(result.code).toBe("invalid_input");
    expect(result.message).toBe("something broke");
  });

  it("handles unknown input", () => {
    const result = parseAuthError("random string");
    expect(result.code).toBe("invalid_input");
    expect(result.status).toBe(500);
  });
});

describe("getErrorMessage", () => {
  it("returns known message for recognized codes", () => {
    expect(getErrorMessage({ code: "locked_out", message: "", status: 0 })).toContain("locked");
  });

  it("falls back to error message for unknown codes", () => {
    expect(getErrorMessage({ code: "something_unknown" as never, message: "custom msg", status: 0 })).toBe("custom msg");
  });
});

describe("normalizeOtpCode", () => {
  it("strips non-digits", () => {
    expect(normalizeOtpCode("abc123def456")).toBe("123456");
  });

  it("converts Persian digits", () => {
    expect(normalizeOtpCode("۱۲۳۴۵۶")).toBe("123456");
  });

  it("respects maxLength", () => {
    expect(normalizeOtpCode("12345678", 4)).toBe("1234");
  });
});

describe("getOtpExpiryPresentation", () => {
  it("returns null for no expiry", () => {
    const result = getOtpExpiryPresentation(null);
    expect(result.remainingSeconds).toBeNull();
    expect(result.isExpired).toBe(false);
  });

  it("detects expired code", () => {
    const past = new Date(Date.now() - 10000).toISOString();
    const result = getOtpExpiryPresentation(past);
    expect(result.isExpired).toBe(true);
    expect(result.remainingSeconds).toBe(0);
  });
});

describe("getRequestedReturnUrl", () => {
  it("returns undefined for empty params", () => {
    expect(getRequestedReturnUrl(new URLSearchParams())).toBeUndefined();
  });

  it("returns undefined for root path", () => {
    expect(getRequestedReturnUrl(new URLSearchParams("returnUrl=/"))).toBeUndefined();
  });

  it("returns undefined for login paths", () => {
    expect(getRequestedReturnUrl(new URLSearchParams("returnUrl=/login"))).toBeUndefined();
  });

  it("returns valid return URL", () => {
    expect(getRequestedReturnUrl(new URLSearchParams("returnUrl=/dashboard"))).toBe("/dashboard");
  });
});
