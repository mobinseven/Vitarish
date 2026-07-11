import { usersSearchUsers, type UserSummaryResponse } from "@vitarish/auth-web";

export type SeaRoleUserSummary = UserSummaryResponse;

export async function searchSeaRoleUsers(query: string): Promise<SeaRoleUserSummary[]> {
  const trimmedQuery = query.trim();

  if (!trimmedQuery) {
    return [];
  }

  return usersSearchUsers({
    Query: trimmedQuery,
    MaxResults: 10,
    IncludeTenantUsers: true,
  });
}
