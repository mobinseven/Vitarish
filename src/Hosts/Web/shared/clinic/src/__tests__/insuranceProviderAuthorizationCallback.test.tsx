import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook } from "@testing-library/react";
import type { PropsWithChildren } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useCompleteInsuranceProviderAuthorization } from "../hooks/settings/insurance/useInsuranceProviderSetup";
import { medicalCompleteProviderAuthorization } from "../api/generated/medical/medical";

vi.mock("../api/generated/medical/medical", () => ({
  medicalCompleteProviderAuthorization: vi.fn(),
}));

const oauthCredentialKey = "vitarish:tamin-oauth-credential";

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { mutations: { retry: false } },
  });

  return function Wrapper({ children }: PropsWithChildren) {
    return (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
  };
}

beforeEach(() => {
  window.history.replaceState({}, "", "/settings/insurance/callback");
  sessionStorage.clear();
  vi.mocked(medicalCompleteProviderAuthorization).mockReset();
});

afterEach(() => {
  sessionStorage.clear();
});

describe("useCompleteInsuranceProviderAuthorization", () => {
  it("completes the correlated callback and removes its temporary reference", async () => {
    window.history.replaceState(
      {},
      "",
      "/settings/insurance/callback?code=authorization-code&state=oauth-state",
    );
    sessionStorage.setItem(oauthCredentialKey, "credential-id");
    vi.mocked(medicalCompleteProviderAuthorization).mockResolvedValue({
      accepted: true,
    });

    const { result } = renderHook(
      () => useCompleteInsuranceProviderAuthorization(),
      { wrapper: createWrapper() },
    );

    await act(() => result.current.mutateAsync());

    expect(medicalCompleteProviderAuthorization).toHaveBeenCalledWith({
      credentialId: "credential-id",
      authorizationCode: "authorization-code",
      state: "oauth-state",
    });
    expect(sessionStorage.getItem(oauthCredentialKey)).toBeNull();
  });

  it("rejects an uncorrelated callback without contacting Medical", async () => {
    window.history.replaceState(
      {},
      "",
      "/settings/insurance/callback?code=authorization-code&state=oauth-state",
    );

    const { result } = renderHook(
      () => useCompleteInsuranceProviderAuthorization(),
      { wrapper: createWrapper() },
    );

    await expect(result.current.mutateAsync()).rejects.toThrow(
      "Provider callback is incomplete or no longer valid.",
    );
    expect(medicalCompleteProviderAuthorization).not.toHaveBeenCalled();
  });
});
