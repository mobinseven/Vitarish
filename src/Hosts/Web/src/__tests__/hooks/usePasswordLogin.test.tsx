import { renderHook, act, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { http, HttpResponse } from "msw";
import { server } from "../server";
import { handlers } from "@/api/generated/authentication/authentication.msw";
import { usePasswordLogin } from "@/hooks/auth/usePasswordLogin";

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe("usePasswordLogin", () => {
  beforeEach(() => {
    queryClient.clear();
  });

  it("should login successfully and return redirect URL", async () => {
    const { result } = renderHook(() => usePasswordLogin(), { wrapper });

    await act(async () => {
      await result.current.login("user@test.com", "password123");
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.error).toBeNull();
  });

  it("should handle invalid credentials error", async () => {
    server.use(
      http.post("http://localhost:5000/api/modules/authentication/login/password", () => {
        return HttpResponse.json(
          {
            status: 401,
            title: "InvalidCredentials",
            detail: "Invalid username or password",
            code: "invalid_credentials",
          },
          { status: 401 }
        );
      })
    );

    const { result } = renderHook(() => usePasswordLogin(), { wrapper });

    await act(async () => {
      await result.current.login("user@test.com", "wrongpassword");
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.error).not.toBeNull();
    expect(result.current.error?.code).toBe("invalid_credentials");
    expect(result.current.errorMessage).toBe("Invalid username or password");

    server.use(...handlers);
  });
});
