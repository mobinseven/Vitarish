export { usersSearchUsers } from "./api/generated/users/users";
export type { UserSummaryResponse } from "./auth/userTypes";

export const tenantRoleSuffixes = {
  owner: "TenantOwner",
  user: "TenantUser",
} as const;

export type TenantRoleSuffix = (typeof tenantRoleSuffixes)[keyof typeof tenantRoleSuffixes];
