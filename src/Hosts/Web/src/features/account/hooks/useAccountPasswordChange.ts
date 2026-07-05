import { useState, useCallback } from "react";
import { apiFetch } from "@/api/client/apiFetch";

interface PasswordChangeResult {
  succeeded: boolean;
}

export function useAccountPasswordChange() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const changePassword = useCallback(
    async (currentPassword: string, newPassword: string) => {
      setError(null);
      setSuccess(false);
      setIsLoading(true);
      try {
        await apiFetch<PasswordChangeResult>(
          "/api/modules/authentication/account/password",
          {
            method: "POST",
            body: JSON.stringify({ currentPassword, newPassword }),
          }
        );
        setSuccess(true);
      } catch (err: unknown) {
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  return {
    isLoading,
    error,
    setError,
    success,
    setSuccess,
    changePassword,
  };
}
