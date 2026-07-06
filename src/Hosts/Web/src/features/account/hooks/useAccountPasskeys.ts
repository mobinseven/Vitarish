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
import {
  PasskeyRegistrationStageError,
  type PasskeyRegistrationStage,
} from "./passkeyRegistrationStageError";

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
    const optionsJson = await runRegistrationStage("begin", () =>
      beginRegister.mutateAsync({ userId })
    );
    const options = await runRegistrationStage("parseOptions", () =>
      parsePasskeyRegistrationOptions(optionsJson)
    );
    const credential = await runRegistrationStage("browserCreate", () =>
      startRegistration(options)
    );
    await runRegistrationStage("complete", () =>
      completeRegister.mutateAsync({
        userId,
        data: { attestationResponseJson: JSON.stringify(credential) },
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

async function runRegistrationStage<T>(
  stage: PasskeyRegistrationStage,
  action: () => T | Promise<T>
): Promise<T> {
  try {
    return await action();
  } catch (error) {
    throw new PasskeyRegistrationStageError(stage, error);
  }
}
