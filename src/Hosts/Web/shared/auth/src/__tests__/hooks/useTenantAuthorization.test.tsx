import { describe, expect, it } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { http, HttpResponse } from "msw";
import { server } from "../server";
import { useTenantAuthorization } from "../../hooks/useTenantAuthorization";

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return function wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
  };
}

function authenticatedSessionWithoutTenantAccess() {
  server.use(
    http.get(/\/api\/modules\/authentication\/session(?:\?|$)/, () =>
      HttpResponse.json({
        isAuthenticated: true,
        user: {
          userId: "user-1",
          displayName: "Avery Stone",
          givenName: "Ave",
          familyName: "Stone",
          email: "avery@example.com",
          phoneNumber: null,
        },
        tenantAccess: {
          canAccessTenantOwner: false,
          canAccessTenantUser: false,
        },
      }),
    ),
  );
}

describe("useTenantAuthorization", () => {
  it("denies owner access when centralized tenant owner access is absent", async () => {
    authenticatedSessionWithoutTenantAccess();

    const { result } = renderHook(() => useTenantAuthorization("owner"), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.hasAccess).toBe(false);
  });

  it("denies user access when centralized tenant user access is absent", async () => {
    authenticatedSessionWithoutTenantAccess();

    const { result } = renderHook(() => useTenantAuthorization("user"), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.hasAccess).toBe(false);
  });
});
