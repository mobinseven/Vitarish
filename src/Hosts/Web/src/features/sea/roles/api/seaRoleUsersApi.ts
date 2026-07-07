import { usersSearchUsers } from "@/api/generated/users/users";
import type { UserSummaryResponse } from "@/api/generated/users/model";

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
