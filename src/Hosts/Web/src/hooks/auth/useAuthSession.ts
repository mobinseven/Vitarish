import { useGetAuthenticationSessionQuery } from "@/api/generated/authentication/authentication";
import { resolveUserDisplayProfile } from "@/features/auth/session/userDisplayProfile";

export function useAuthSession() {
  const query = useGetAuthenticationSessionQuery();

  const user = query.data?.user ?? null;

  return {
    session: query.data,
    isAuthenticated: query.data?.isAuthenticated ?? false,
    user,
    displayProfile: resolveUserDisplayProfile(user),
    isLoading: query.isLoading,
    isError: query.isError,
  };
}
