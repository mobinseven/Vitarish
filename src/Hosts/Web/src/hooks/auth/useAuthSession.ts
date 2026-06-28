import { useGetAuthenticationSessionQuery } from "@/api/generated/authentication/authentication";

export function useAuthSession() {
  const query = useGetAuthenticationSessionQuery();

  return {
    session: query.data,
    isAuthenticated: query.data?.isAuthenticated ?? false,
    user: query.data?.user ?? null,
    isLoading: query.isLoading,
    isError: query.isError,
  };
}
