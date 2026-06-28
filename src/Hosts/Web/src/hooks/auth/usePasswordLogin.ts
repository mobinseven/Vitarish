import { useCallback, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { usePasswordLoginMutation } from "@/api/generated/authentication/authentication";
import { parseAuthError, getErrorMessage, type AuthError } from "@/lib/errors";

export function usePasswordLogin() {
  const router = useRouter();
  const searchParams = useSearchParams();
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
        const finalReturnUrl = returnUrl ?? searchParams.get("returnUrl") ?? "/";
        const result = await mutation.mutateAsync({
          userNameOrEmail,
          password,
          returnUrl: finalReturnUrl,
          rememberMe: false,
        });
        router.push(result.returnUrl);
      } catch (err) {
        const authError = parseAuthError(err);
        setError(authError);
      }
    },
    [mutation, router, searchParams]
  );

  return {
    login,
    isLoading: mutation.isPending,
    error,
    errorMessage: error ? getErrorMessage(error) : null,
  };
}
