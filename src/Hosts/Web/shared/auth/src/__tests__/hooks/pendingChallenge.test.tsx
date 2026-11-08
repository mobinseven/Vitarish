import { describe, it, expect } from "vitest";
import { act, renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { http, HttpResponse } from "msw";
import { server } from "../server";
import { useEmailLogin } from "../../hooks/useEmailLogin";
import { getAuthenticationPendingChallengeGetQueryKey } from "../../api/generated/authentication/authentication";

function wrapper({ children }: { children: ReactNode }) {
  return <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>{children}</QueryClientProvider>;
}

describe("pending authentication challenge recovery", () => {
  it("restores email verification after remount without sending another code", async () => {
    let deliveries = 0;
    server.use(
      http.get(`*${getAuthenticationPendingChallengeGetQueryKey()[0]}`, () => HttpResponse.json({
        challenge: { challengeId: "pending-email", purpose: 1, expiresAt: "2026-11-07T12:00:00Z", remainingAttempts: 4 },
        destination: "owner@example.test", returnUrl: "/continue?request=opaque",
        isEmailLogin: true, isPhoneLogin: false, isPasswordOtp: false, isPasswordRecovery: false,
      })),
      http.post(/\/login\/email\/begin$/, () => { deliveries++; return new HttpResponse(null, { status: 500 }); }),
    );
    const first = renderHook(() => useEmailLogin(), { wrapper });
    await waitFor(() => expect(first.result.current.step).toBe("code"));
    expect(first.result.current.email).toBe("owner@example.test");
    expect(first.result.current.remainingAttempts).toBe(4);
    first.unmount();
    const restored = renderHook(() => useEmailLogin(), { wrapper });
    await waitFor(() => expect(restored.result.current.step).toBe("code"));
    expect(restored.result.current.challenge?.challengeId).toBe("pending-email");
    expect(deliveries).toBe(0);
  });

  it("does not replace a newly started challenge with a late recovery response", async () => {
    let release!: () => void;
    const pending = new Promise<void>(resolve => { release = resolve; });
    server.use(http.get(`*${getAuthenticationPendingChallengeGetQueryKey()[0]}`, async () => {
      await pending;
      return HttpResponse.json({
        challenge: { challengeId: "old-email", purpose: 1, expiresAt: "2026-11-07T12:00:00Z", remainingAttempts: 4 },
        destination: "old@example.test", returnUrl: null,
        isEmailLogin: true, isPhoneLogin: false, isPasswordOtp: false, isPasswordRecovery: false,
      });
    }));
    const { result } = renderHook(() => useEmailLogin(), { wrapper });
    await act(async () => { await result.current.begin("new@example.test"); });
    await act(async () => { release(); await pending; });
    await waitFor(() => expect(result.current.isLoading).toBe(false));
    expect(result.current.email).toBe("new@example.test");
    expect(result.current.challenge?.challengeId).not.toBe("old-email");
  });
});
