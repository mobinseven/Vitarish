import { act, renderHook } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  parsePasskeyRegistrationOptions,
  PasskeyRegistrationOptionsError,
} from "@/features/account/hooks/passkeyRegistrationOptions";
import { PasskeyRegistrationStageError } from "@/features/account/hooks/passkeyRegistrationStageError";
import { useAccountPasskeys } from "@/features/account/hooks/useAccountPasskeys";
import { startRegistration } from "@/lib/webauthn";

const beginRegisterMutateAsync = vi.fn();
const completeRegisterMutateAsync = vi.fn();
const deleteCredentialMutateAsync = vi.fn();

vi.mock("@/api/generated/users/users", () => ({
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

vi.mock("@/lib/webauthn", async () => {
  const actual = await vi.importActual<typeof import("@/lib/webauthn")>("@/lib/webauthn");
  return {
    ...actual,
    startRegistration: vi.fn(),
  };
});

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
  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}

describe("parsePasskeyRegistrationOptions", () => {
  it("rejects malformed begin-register JSON", () => {
    expect(() => parsePasskeyRegistrationOptions("{")).toThrow(
      PasskeyRegistrationOptionsError
    );
  });

  it("rejects missing required WebAuthn option fields", () => {
    const missingChallenge = { ...validOptions, challenge: "" };

    expect(() => parsePasskeyRegistrationOptions(JSON.stringify(missingChallenge))).toThrow(
      PasskeyRegistrationOptionsError
    );
  });

  it("accepts required WebAuthn creation options", () => {
    expect(parsePasskeyRegistrationOptions(JSON.stringify(validOptions))).toEqual(validOptions);
  });
});

describe("useAccountPasskeys registration stages", () => {
  beforeEach(() => {
    queryClient.clear();
    beginRegisterMutateAsync.mockReset();
    completeRegisterMutateAsync.mockReset();
    deleteCredentialMutateAsync.mockReset();
    vi.mocked(startRegistration).mockReset();
  });

  it("wraps browser passkey creation failures after begin-register succeeds", async () => {
    const browserError = new Error("browser create failed");
    beginRegisterMutateAsync.mockResolvedValue(JSON.stringify(validOptions));
    vi.mocked(startRegistration).mockRejectedValue(browserError);

    const { result } = renderHook(() => useAccountPasskeys("user-1"), { wrapper });

    await expect(
      act(async () => {
        await result.current.register();
      })
    ).rejects.toMatchObject({
      stage: "browserCreate",
      cause: browserError,
    });
  });

  it("wraps complete-register failures after browser passkey creation succeeds", async () => {
    const completeError = new Error("complete failed");
    beginRegisterMutateAsync.mockResolvedValue(JSON.stringify(validOptions));
    vi.mocked(startRegistration).mockResolvedValue({ id: "credential-1" } as never);
    completeRegisterMutateAsync.mockRejectedValue(completeError);

    const { result } = renderHook(() => useAccountPasskeys("user-1"), { wrapper });

    await expect(
      act(async () => {
        await result.current.register();
      })
    ).rejects.toMatchObject({
      stage: "complete",
      cause: completeError,
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
