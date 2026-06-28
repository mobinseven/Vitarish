import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import {
  useLogoutMutation,
  useGetLogoutContextQuery,
} from "@/api/generated/authentication/authentication";
import { parseAuthError, getErrorMessage, type AuthError } from "@/lib/errors";

export function useLogout(logoutId?: string) {
  const router = useRouter();
  const mutation = useLogoutMutation();
  const contextQuery = useGetLogoutContextQuery(logoutId ?? null);
  const [error, setError] = useState<AuthError | null>(null);

  const logout = useCallback(
    async (returnUrl?: string) => {
      setError(null);
      try {
        const result = await mutation.mutateAsync({
          logoutId: logoutId ?? null,
          returnUrl: returnUrl ?? "/login",
        });
        if (result.redirectUrl) {
          router.push(result.redirectUrl);
        } else {
          router.push("/login");
        }
      } catch (err) {
        setError(parseAuthError(err));
      }
    },
    [mutation, logoutId, router]
  );

  return {
    logout,
    context: contextQuery.data,
    isLoading: mutation.isPending,
    error,
    errorMessage: error ? getErrorMessage(error) : null,
  };
}
