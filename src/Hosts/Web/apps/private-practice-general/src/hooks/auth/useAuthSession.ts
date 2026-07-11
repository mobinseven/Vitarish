import { useMemo } from "react";
import { useAuthenticationSessionGet } from "@/api/generated/authentication/authentication";
import { resolveUserDisplayProfile } from "@vitarish/auth-web";

export function useAuthSession() {
  const query = useAuthenticationSessionGet();

  const user = query.data?.user ?? null;
  const displayProfile = useMemo(() => resolveUserDisplayProfile(user), [user]);

  return {
    session: query.data,
    isAuthenticated: query.data?.isAuthenticated ?? false,
    user,
    displayProfile,
    isLoading: query.isLoading,
    isError: query.isError,
  };
}
