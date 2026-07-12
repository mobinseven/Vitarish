import { beforeEach, describe, expect, it } from "vitest";
import { renderHook, act, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { usePhoneLogin } from "../../hooks/usePhoneLogin";

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe("usePhoneLogin", () => {
  beforeEach(() => {
    queryClient.clear();
  });

  it("should start in phone step", () => {
    const { result } = renderHook(() => usePhoneLogin(), { wrapper });
    expect(result.current.step).toBe("phone");
  });

  it("should transition to code step after begin", async () => {
    const { result } = renderHook(() => usePhoneLogin(), { wrapper });

    await act(async () => {
      await result.current.begin("09121234567");
    });

    await waitFor(() => {
      expect(result.current.step).toBe("code");
    });

    expect(result.current.challenge).not.toBeNull();
    expect(result.current.challenge?.challengeId).toBe("test-challenge-id");
  });

  it("should reset back to phone step", async () => {
    const { result } = renderHook(() => usePhoneLogin(), { wrapper });

    await act(async () => {
      await result.current.begin("09121234567");
    });

    await waitFor(() => {
      expect(result.current.step).toBe("code");
    });

    act(() => {
      result.current.reset();
    });

    expect(result.current.step).toBe("phone");
    expect(result.current.challenge).toBeNull();
  });

  it("should resend the current phone challenge through begin", async () => {
    const { result } = renderHook(() => usePhoneLogin(), { wrapper });

    await act(async () => {
      await result.current.begin("09121234567");
    });

    await act(async () => {
      await result.current.resend();
    });

    expect(result.current.step).toBe("code");
    expect(result.current.statusMessage).toBe("A new code was sent to your phone number.");
  });
});
