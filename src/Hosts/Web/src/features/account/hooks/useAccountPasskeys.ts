import { useState, useCallback, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  useUsersListWebAuthnCredentials,
  useUsersBeginWebAuthnRegister,
  useUsersCompleteWebAuthnRegister,
  useUsersDeleteWebAuthnCredential,
} from "@/api/generated/users/users";
import { startRegistration } from "@/lib/webauthn";
import { accountKeys } from "./accountKeys";

import { parsePasskeyRegistrationOptions } from "./passkeyRegistrationOptions";

export function useAccountPasskeys(userId: string) {
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
    const optionsJson = await beginRegister.mutateAsync({ userId });
    const options = parsePasskeyRegistrationOptions(optionsJson);
    const credential = await startRegistration(options);
    await completeRegister.mutateAsync({
      userId,
      data: { attestationResponseJson: JSON.stringify(credential) },
    });
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
