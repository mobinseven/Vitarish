import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { http, HttpResponse } from "msw";
import { server } from "../server";
import { useAuthSession } from "@/hooks/auth/useAuthSession";

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe("useAuthSession", () => {
  beforeEach(() => {
    queryClient.clear();
  });

  it("should return session data", async () => {
    const { result } = renderHook(() => useAuthSession(), { wrapper });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
    expect(result.current.displayProfile.displayLabel).toBe("friend");
  });

  it("prefers authenticated user given name in the display profile", async () => {
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
        })
      )
    );

    const { result } = renderHook(() => useAuthSession(), { wrapper });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.displayProfile.displayLabel).toBe("Avery Stone");
    expect(result.current.displayProfile.givenName).toBe("Ave");
    expect(result.current.displayProfile.familyName).toBe("Stone");
  });

  it("memoizes the fallback display profile while the user reference is unchanged", async () => {
    const { result, rerender } = renderHook(() => useAuthSession(), { wrapper });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const displayProfile = result.current.displayProfile;

    rerender();

    expect(result.current.displayProfile).toBe(displayProfile);
  });
});
