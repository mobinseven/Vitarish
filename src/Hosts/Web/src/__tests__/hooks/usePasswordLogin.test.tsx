import { renderHook, act, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { http, HttpResponse } from "msw";
import { server } from "../server";
import { handlers } from "@/api/generated/authentication/authentication.msw";
import { usePasswordLogin } from "@/hooks/auth/usePasswordLogin";
import { authenticationKeys } from "@/api/generated/authentication/authentication";
import { useAuthSession } from "@/hooks/auth/useAuthSession";

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

  it("does not send a fabricated root return URL when none is requested", async () => {
    let requestBody: Record<string, unknown> | undefined;
    server.use(
      http.post(
        "https://localhost:7272/api/modules/authentication/login/password",
        async ({ request }) => {
          requestBody = (await request.json()) as Record<string, unknown>;
          return HttpResponse.json({
            returnUrl: "/home",
            requiresProfileCompletion: false,
          });
        }
      )
    );

    const { result } = renderHook(() => usePasswordLogin(), { wrapper });

    await act(async () => {
      await result.current.login("user@test.com", "password123");
    });

    expect(requestBody).not.toHaveProperty("returnUrl");
  });

  it("ignores login pages as explicit post-login return URLs", async () => {
    let requestBody: Record<string, unknown> | undefined;
    server.use(
      http.post(
        "https://localhost:7272/api/modules/authentication/login/password",
        async ({ request }) => {
          requestBody = (await request.json()) as Record<string, unknown>;
          return HttpResponse.json({
            returnUrl: "/home",
            requiresProfileCompletion: false,
          });
        }
      )
    );

    const { result } = renderHook(() => usePasswordLogin(), { wrapper });

    await act(async () => {
      await result.current.login(
        "user@test.com",
        "password123",
        "/login/password"
      );
    });

    expect(requestBody).not.toHaveProperty("returnUrl");
  });

  it("invalidates the authentication session query after successful login", async () => {
    const invalidateSpy = vi.spyOn(queryClient, "invalidateQueries");
    const { result } = renderHook(() => usePasswordLogin(), { wrapper });

    await act(async () => {
      await result.current.login("user@test.com", "password123");
    });

    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: authenticationKeys.session(),
    });
  });

  it("refreshes a cached unauthenticated session after successful login", async () => {
    let isAuthenticated = false;
    server.use(
      http.get(
        "https://localhost:7272/api/modules/authentication/session",
        () =>
          HttpResponse.json({
            isAuthenticated,
            user: isAuthenticated
              ? {
                  userId: "user-1",
                  displayName: "Avery Stone",
                  givenName: "Avery",
                  familyName: "Stone",
                  email: "avery@example.com",
                  phoneNumber: null,
                }
              : null,
          })
      ),
      http.post(
        "https://localhost:7272/api/modules/authentication/login/password",
        () => {
          isAuthenticated = true;
          return HttpResponse.json({
            returnUrl: "/home",
            requiresProfileCompletion: false,
          });
        }
      )
    );

    const { result } = renderHook(
      () => ({ login: usePasswordLogin(), session: useAuthSession() }),
      { wrapper }
    );

    await waitFor(() => {
      expect(result.current.session.isAuthenticated).toBe(false);
    });

    await act(async () => {
      await result.current.login.login("user@test.com", "password123");
    });

    await waitFor(() => {
      expect(result.current.session.isAuthenticated).toBe(true);
      expect(result.current.session.displayProfile.givenName).toBe("Avery");
    });
  });

  it("should handle invalid credentials error", async () => {
    server.use(
      http.post(
        "https://localhost:7272/api/modules/authentication/login/password",
        () => {
          return HttpResponse.json(
          {
            status: 401,
            title: "InvalidCredentials",
            detail: "Invalid username or password",
            code: "invalid_credentials",
          },
            { status: 401 }
          );
        }
      )
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
