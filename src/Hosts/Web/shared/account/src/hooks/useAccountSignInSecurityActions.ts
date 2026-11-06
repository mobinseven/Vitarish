import { useCallback } from "react";
import {
  useAuthenticationAccountPasswordFirstSet,
  useAuthenticationAccountTwoStepSignInChange,
} from "@vitarish/auth";

export function useAccountSignInSecurityActions() {
  const firstPasswordMutation = useAuthenticationAccountPasswordFirstSet();
  const twoStepMutation = useAuthenticationAccountTwoStepSignInChange();

  const setFirstPassword = useCallback(
    async (newPassword: string) => {
      const result = await firstPasswordMutation.mutateAsync({
        data: { newPassword },
      });
      if (!result.succeeded) throw new Error();
    },
    [firstPasswordMutation.mutateAsync],
  );

  const setTwoFactorEnabled = useCallback(
    async (enabled: boolean) => {
      const result = await twoStepMutation.mutateAsync({
        data: { enabled },
      });
      if (!result.succeeded) throw new Error();
    },
    [twoStepMutation.mutateAsync],
  );

  return { setFirstPassword, setTwoFactorEnabled };
}
