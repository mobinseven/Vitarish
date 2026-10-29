import { beforeEach, describe, expect, it } from "vitest";
import { renderHook, act, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { http, HttpResponse } from "msw";
import { server } from "../server";
import { useEmailLogin } from "../../hooks/useEmailLogin";
import { usePhoneLogin } from "../../hooks/usePhoneLogin";
import { useAuthSession } from "../../hooks/useAuthSession";

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

// A page that read the session before sign-in must not keep that signed-out
// answer once the code is verified, or its guard sends the person back to login.
describe.each([
  { method: "email", useCodeLogin: useEmailLogin, destination: "person@example.com" },
  { method: "phone", useCodeLogin: usePhoneLogin, destination: "09121234567" },
])("$method code sign-in", ({ method, useCodeLogin, destination }) => {
  beforeEach(() => {
    queryClient.clear();
  });

  it("refreshes a cached unauthenticated session after the code is verified", async () => {
    let isAuthenticated = false;
    server.use(
      http.get(/\/api\/modules\/authentication\/session(?:\?|$)/, () =>
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
        }),
      ),
      http.post(new RegExp(`/api/modules/authentication/login/${method}/complete(?:\\?|$)`), () => {
        isAuthenticated = true;
        return HttpResponse.json({ returnUrl: "/home", requiresProfileCompletion: false });
      }),
    );

    const { result } = renderHook(
      () => ({ login: useCodeLogin(), session: useAuthSession() }),
      { wrapper },
    );

    await waitFor(() => {
      expect(result.current.session.isAuthenticated).toBe(false);
    });

    await act(async () => {
      await result.current.login.begin(destination);
    });
    await act(async () => {
      await result.current.login.complete("123456");
    });

    await waitFor(() => {
      expect(result.current.session.isAuthenticated).toBe(true);
      expect(result.current.session.displayProfile.givenName).toBe("Avery");
    });
  });
});
