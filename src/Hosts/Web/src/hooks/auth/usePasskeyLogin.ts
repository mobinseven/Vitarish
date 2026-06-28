import { useCallback, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  useBeginPasskeyLoginMutation,
  useCompletePasskeyLoginMutation,
} from "@/api/generated/authentication/authentication";
import { startPasskeyAuthentication } from "@/lib/webauthn";
import { parseAuthError, getErrorMessage, type AuthError } from "@/lib/errors";

export function usePasskeyLogin() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const beginMutation = useBeginPasskeyLoginMutation();
  const completeMutation = useCompletePasskeyLoginMutation();
  const [error, setError] = useState<AuthError | null>(null);

  const login = useCallback(
    async (userNameOrEmail?: string, returnUrl?: string) => {
      setError(null);
      try {
        const finalReturnUrl = returnUrl ?? searchParams.get("returnUrl") ?? "/";
        const challenge = await beginMutation.mutateAsync({
          userNameOrEmail: userNameOrEmail || null,
          returnUrl: finalReturnUrl,
        });

        if (!challenge.optionsJson) {
          throw new Error("No passkey options received");
        }

        const credentialJson = await startPasskeyAuthentication(
          challenge.optionsJson
        );

        const result = await completeMutation.mutateAsync({
          challengeId: challenge.challengeId,
          credentialJson,
          returnUrl: finalReturnUrl,
        });

        router.push(result.returnUrl);
      } catch (err) {
        const authError = parseAuthError(err);
        setError(authError);
      }
    },
    [beginMutation, completeMutation, router, searchParams]
  );

  return {
    login,
    isLoading: beginMutation.isPending || completeMutation.isPending,
    error,
    errorMessage: error ? getErrorMessage(error) : null,
  };
}
