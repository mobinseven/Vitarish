import { useState, useCallback, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  useUsersListWebAuthnCredentials,
  useUsersBeginWebAuthnRegister,
  useUsersCompleteWebAuthnRegister,
  useUsersDeleteWebAuthnCredential,
  type WebAuthnCredentialResponse,
} from "@vitarish/auth";
import { registerPasskey, startRegistration } from "@vitarish/account";
import { accountKeys } from "./accountKeys";

export interface AccountPasskeysResult {
  credentials: WebAuthnCredentialResponse[] | undefined;
  isLoadingCredentials: boolean;
  isSupported: boolean | null;
  isRegisterPending: boolean;
  isDeletePending: boolean;
  register: () => Promise<void>;
  remove: (credentialId: string) => Promise<unknown>;
}

export function useAccountPasskeys(userId: string): AccountPasskeysResult {
  const queryClient = useQueryClient();
  const { data: credentials, isLoading: isLoadingCredentials } =
    useUsersListWebAuthnCredentials(userId);
  const beginRegister = useUsersBeginWebAuthnRegister();
  const completeRegister = useUsersCompleteWebAuthnRegister({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: accountKeys.webauthn(userId),
        });
      },
    },
  });
  const deleteCredential = useUsersDeleteWebAuthnCredential({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: accountKeys.webauthn(userId),
        });
      },
    },
  });

  const [isSupported, setIsSupported] = useState<boolean | null>(null);

  useEffect(() => {
    setIsSupported(
      typeof window !== "undefined" &&
        window.PublicKeyCredential !== undefined &&
        window.isSecureContext === true &&
        navigator.credentials !== undefined
    );
  }, []);

  const register = useCallback(async () => {
    await registerPasskey(
      startRegistration,
      () => beginRegister.mutateAsync({ userId }),
      (attestationResponseJson) =>
        completeRegister.mutateAsync({
          userId,
          data: { attestationResponseJson },
        })
    );
  }, [beginRegister, completeRegister, userId]);

  const remove = useCallback(
    async (credentialId: string) => {
      await deleteCredential.mutateAsync({ userId, credentialId });
    },
    [deleteCredential, userId]
  );

  return {
    credentials,
    isLoadingCredentials,
    isSupported,
    isRegisterPending: beginRegister.isPending || completeRegister.isPending,
    isDeletePending: deleteCredential.isPending,
    register,
    remove,
  };
}
