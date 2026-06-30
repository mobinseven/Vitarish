import { useCallback, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import {
  authenticationKeys,
  usePasswordLoginMutation,
} from "@/api/generated/authentication/authentication";
import { parseAuthError, getErrorMessage, type AuthError } from "@/lib/errors";
import { getRequestedReturnUrl } from "./requestedReturnUrl";

export function usePasswordLogin() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const queryClient = useQueryClient();
  const mutation = usePasswordLoginMutation();
  const [error, setError] = useState<AuthError | null>(null);

  const login = useCallback(
    async (
      userNameOrEmail: string,
      password: string,
      returnUrl?: string
    ) => {
      setError(null);
      try {
        const finalReturnUrl = getRequestedReturnUrl(searchParams, returnUrl);
        const result = await mutation.mutateAsync({
          userNameOrEmail,
          password,
          returnUrl: finalReturnUrl,
          rememberMe: false,
        });

        queryClient.removeQueries({ queryKey: authenticationKeys.all });

        // Use legacy returnUrl from auth result as fallback
        // Journey destination will be resolved by the gate boundary
        router.push(result.returnUrl);
      } catch (err) {
        const authError = parseAuthError(err);
        setError(authError);
      }
    },
    [mutation, queryClient, router, searchParams]
  );

  return {
    login,
    isLoading: mutation.isPending,
    error,
    errorMessage: error ? getErrorMessage(error) : null,
  };
}
