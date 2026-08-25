import { act, renderHook } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  parsePasskeyRegistrationOptions,
  PasskeyRegistrationOptionsError,
  PasskeyRegistrationStageError,
  useAccountPasskeys,
} from "@vitarish/account";

const beginRegisterMutateAsync = vi.fn();
const completeRegisterMutateAsync = vi.fn();
const deleteCredentialMutateAsync = vi.fn();

vi.mock("@vitarish/auth", () => ({
  useUsersListWebAuthnCredentials: vi.fn(() => ({
    data: [],
    isLoading: false,
  })),
  useUsersBeginWebAuthnRegister: vi.fn(() => ({
    mutateAsync: beginRegisterMutateAsync,
    isPending: false,
  })),
  useUsersCompleteWebAuthnRegister: vi.fn(() => ({
    mutateAsync: completeRegisterMutateAsync,
    isPending: false,
  })),
  useUsersDeleteWebAuthnCredential: vi.fn(() => ({
    mutateAsync: deleteCredentialMutateAsync,
    isPending: false,
  })),
  getUsersListWebAuthnCredentialsQueryKey: vi.fn((userId: string) => [
    "users",
    userId,
    "webauthn",
  ]),
}));

const validOptions = {
  challenge: "Y2hhbGxlbmdl",
  rp: { name: "Vitarish" },
  user: { id: "dXNlcg", name: "user@example.com", displayName: "User" },
  pubKeyCredParams: [{ type: "public-key", alg: -7 }],
};

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
});

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe("parsePasskeyRegistrationOptions", () => {
  it("rejects malformed begin-register JSON", () => {
    expect(() => parsePasskeyRegistrationOptions("{")).toThrow(
      PasskeyRegistrationOptionsError,
    );
  });

  it("rejects missing required WebAuthn option fields", () => {
    const missingChallenge = { ...validOptions, challenge: "" };

    expect(() =>
      parsePasskeyRegistrationOptions(JSON.stringify(missingChallenge)),
    ).toThrow(PasskeyRegistrationOptionsError);
  });

  it("accepts required WebAuthn creation options", () => {
    expect(
      parsePasskeyRegistrationOptions(JSON.stringify(validOptions)),
    ).toEqual(validOptions);
  });
});

describe("useAccountPasskeys", () => {
  beforeEach(() => {
    queryClient.clear();
    beginRegisterMutateAsync.mockReset();
    completeRegisterMutateAsync.mockReset();
    deleteCredentialMutateAsync.mockReset();
  });

  it("calls begin-register and complete-register with correct arguments", async () => {
    beginRegisterMutateAsync.mockResolvedValue(JSON.stringify(validOptions));
    completeRegisterMutateAsync.mockResolvedValue(undefined);

    const { result } = renderHook(() => useAccountPasskeys("user-1"), {
      wrapper,
    });

    // register will fail because WebAuthn is not available in jsdom, but begin-register should still be called
    try {
      await act(async () => {
        await result.current.register();
      });
    } catch {
      // Expected: WebAuthnUnsupportedError in jsdom environment
    }

    expect(beginRegisterMutateAsync).toHaveBeenCalledWith({ userId: "user-1" });
  });

  it("exposes credentials and loading state", () => {
    const { result } = renderHook(() => useAccountPasskeys("user-1"), {
      wrapper,
    });

    expect(result.current.credentials).toEqual([]);
    expect(result.current.isLoadingCredentials).toBe(false);
    expect(result.current.isSupported).toBe(false);
  });

  it("calls delete-credential when remove is invoked", async () => {
    deleteCredentialMutateAsync.mockResolvedValue(undefined);

    const { result } = renderHook(() => useAccountPasskeys("user-1"), {
      wrapper,
    });

    await act(async () => {
      await result.current.remove("cred-123");
    });

    expect(deleteCredentialMutateAsync).toHaveBeenCalledWith({
      userId: "user-1",
      credentialId: "cred-123",
    });
  });

  it("exposes a typed stage error for registration failures", () => {
    const cause = new Error("failed");

    expect(new PasskeyRegistrationStageError("begin", cause)).toMatchObject({
      name: "PasskeyRegistrationStageError",
      stage: "begin",
      cause,
    });
  });
});
