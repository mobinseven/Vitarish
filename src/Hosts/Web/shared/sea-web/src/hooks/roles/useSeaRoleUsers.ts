import { useQuery } from "@tanstack/react-query";
import { searchSeaRoleUsers } from "../../api/roles/seaRoleUsersApi";

export const seaRoleUserKeys = {
  all: ["sea", "role-users"] as const,
  search: (query: string) => [...seaRoleUserKeys.all, "search", query.trim()] as const,
};

export function useSeaRoleUserSearch(query: string) {
  const trimmedQuery = query.trim();

  return useQuery({
    queryKey: seaRoleUserKeys.search(trimmedQuery),
    queryFn: () => searchSeaRoleUsers(trimmedQuery),
    enabled: trimmedQuery.length >= 2,
  });
}
