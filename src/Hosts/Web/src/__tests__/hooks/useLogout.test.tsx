import { renderHook, act, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { http, HttpResponse } from "msw";
import { server } from "../server";
import { authenticationKeys } from "@/api/generated/authentication/authentication";
import { useLogout } from "@/hooks/auth/useLogout";

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe("useLogout", () => {
  beforeEach(() => {
    queryClient.clear();
  });

  it("reports loading until logout context is available", async () => {
    const { result } = renderHook(() => useLogout("logout-1"), { wrapper });

    expect(result.current.contextStatus).toBe("loading");

    await waitFor(() => {
      expect(result.current.isContextLoading).toBe(false);
    });

    expect(result.current.contextStatus).toBe("ready");
    expect(result.current.context).toMatchObject({ showLogoutPrompt: false });
  });

  it("maps logout context 401 to an already-signed-out state", async () => {
    server.use(
      http.get(
        "https://localhost:7272/api/modules/authentication/logout/context",
        () => HttpResponse.json({ status: 401, title: "Unauthorized" }, { status: 401 })
      )
    );

    const { result } = renderHook(() => useLogout("expired"), { wrapper });

    await waitFor(() => {
      expect(result.current.isContextLoading).toBe(false);
    });

    expect(result.current.contextStatus).toBe("already-signed-out");
  });

  it("logs out, removes auth queries, and navigates to login", async () => {
    queryClient.setQueryData(authenticationKeys.session(), {
      isAuthenticated: true,
      user: { userId: "user-1", displayName: "Avery Stone" },
    });

    const { result } = renderHook(() => useLogout(), { wrapper });

    await act(async () => {
      await result.current.logout();
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(queryClient.getQueryData(authenticationKeys.session())).toBeUndefined();
    expect(result.current.error).toBeNull();
  });
});
