import { afterEach, describe, expect, it, vi } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { useOtpCodeCapture } from "../../hooks/useOtpCodeCapture";

describe("useOtpCodeCapture", () => {
  const originalCredentials = navigator.credentials;

  afterEach(() => {
    vi.restoreAllMocks();
    Reflect.deleteProperty(window, "OTPCredential");
    Object.defineProperty(navigator, "credentials", {
      configurable: true,
      value: originalCredentials,
    });
  });

  it("falls back without emitting when Web OTP is unsupported", () => {
    const onCode = vi.fn();

    renderHook(() => useOtpCodeCapture({ enabled: true, onCode }));

    expect(onCode).not.toHaveBeenCalled();
  });

  it("emits a normalized complete SMS code", async () => {
    const onCode = vi.fn();
    Object.defineProperty(window, "OTPCredential", { configurable: true, value: function OTPCredential() {} });
    Object.defineProperty(navigator, "credentials", {
      configurable: true,
      value: { get: vi.fn().mockResolvedValue({ code: "کد ۱۲٣456" }) },
    });

    renderHook(() => useOtpCodeCapture({ enabled: true, onCode }));

    await waitFor(() => expect(onCode).toHaveBeenCalledWith("123456"));
  });

  it("aborts capture on cleanup", () => {
    let signal: AbortSignal | undefined;
    Object.defineProperty(window, "OTPCredential", { configurable: true, value: function OTPCredential() {} });
    Object.defineProperty(navigator, "credentials", {
      configurable: true,
      value: {
        get: vi.fn((options: { signal: AbortSignal }) => {
          signal = options.signal;
          return new Promise(() => undefined);
        }),
      },
    });

    const { unmount } = renderHook(() => useOtpCodeCapture({ enabled: true, onCode: vi.fn() }));
    unmount();

    expect(signal?.aborted).toBe(true);
  });

  it("rejects invalid or incomplete capture output", async () => {
    const onCode = vi.fn();
    Object.defineProperty(window, "OTPCredential", { configurable: true, value: function OTPCredential() {} });
    Object.defineProperty(navigator, "credentials", {
      configurable: true,
      value: { get: vi.fn().mockResolvedValue({ code: "12ab" }) },
    });

    renderHook(() => useOtpCodeCapture({ enabled: true, onCode }));

    await waitFor(() => expect(navigator.credentials?.get).toHaveBeenCalled());
    expect(onCode).not.toHaveBeenCalled();
  });
});
