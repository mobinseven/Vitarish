import { beforeEach, describe, expect, it } from "vitest";
import { act, renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { http, HttpResponse } from "msw";
import { getAuthenticationSessionGetQueryKey } from "../../api/generated/authentication/authentication";
import { server } from "../server";
import { useAuthSession } from "../../hooks/useAuthSession";

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

  it("treats an unauthorized session response as signed out without retrying", async () => {
    let requests = 0;
    server.use(
      http.get(/\/api\/modules\/authentication\/session(?:\?|$)/, () => {
        requests += 1;
        return HttpResponse.json(
          { status: 401, title: "Unauthorized" },
          { status: 401 },
        );
      }),
    );

    const { result } = renderHook(() => useAuthSession(), { wrapper });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.isError).toBe(false);
    expect(requests).toBe(1);
    expect(result.current.session).toBeUndefined();
    expect(result.current.displayProfile.displayLabel).toBe("friend");
  });

  it("clears exposed identity after an unauthorized refetch and restores it after recovery", async () => {
    const authenticatedSession = {
      isAuthenticated: true,
      user: {
        userId: "user-1",
        displayName: "Avery Stone",
        givenName: "Ave",
        familyName: "Stone",
        email: "avery@example.com",
        phoneNumber: null,
      },
      tenantAccess: { canAccessTenantOwner: true, canAccessTenantUser: true },
    };
    let unauthorized = false;
    let requests = 0;
    server.use(
      http.get(`*${getAuthenticationSessionGetQueryKey()[0]}`, () => {
        requests += 1;
        return unauthorized
          ? HttpResponse.json({ status: 401, title: "Unauthorized" }, { status: 401 })
          : HttpResponse.json(authenticatedSession);
      }),
    );

    const { result } = renderHook(() => useAuthSession(), { wrapper });
    await waitFor(() => expect(result.current.isAuthenticated).toBe(true));
    expect(result.current.user).toEqual(authenticatedSession.user);
    expect(result.current.displayProfile.displayLabel).toBe("Avery Stone");

    unauthorized = true;
    await act(async () => {
      await queryClient.refetchQueries({ queryKey: getAuthenticationSessionGetQueryKey() });
    });

    await waitFor(() => expect(result.current.isAuthenticated).toBe(false));
    expect(result.current.session).toBeUndefined();
    expect(result.current.user).toBeNull();
    expect(result.current.displayProfile.displayLabel).toBe("friend");
    expect(result.current.isLoading).toBe(false);
    expect(result.current.isError).toBe(false);
    expect(requests).toBe(2);

    unauthorized = false;
    await act(async () => {
      await queryClient.refetchQueries({ queryKey: getAuthenticationSessionGetQueryKey() });
    });

    await waitFor(() => expect(result.current.isAuthenticated).toBe(true));
    expect(result.current.session).toEqual(authenticatedSession);
    expect(result.current.user).toEqual(authenticatedSession.user);
    expect(result.current.displayProfile.displayLabel).toBe("Avery Stone");
    expect(result.current.isError).toBe(false);
    expect(requests).toBe(3);
  });

  it("keeps non-unauthorized session failures visible as errors", async () => {
    server.use(
      http.get(/\/api\/modules\/authentication\/session(?:\?|$)/, () =>
        HttpResponse.json({ status: 503, title: "Unavailable" }, { status: 503 }),
      ),
    );

    const { result } = renderHook(() => useAuthSession(), { wrapper });

    await waitFor(() => expect(result.current.isLoading).toBe(false), { timeout: 3_000 });
    expect(result.current.isError).toBe(true);

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.session).toBeUndefined();
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
          tenantAccess: {
            canAccessTenantOwner: true,
            canAccessTenantUser: true,
          },
        }),
      ),
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
    const { result, rerender } = renderHook(() => useAuthSession(), {
      wrapper,
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const displayProfile = result.current.displayProfile;

    rerender();

    expect(result.current.displayProfile).toBe(displayProfile);
  });
});
