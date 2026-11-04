export { getUsersAdministrationGetUserSecurityQueryKey, getUsersAdministrationListTenantRolesQueryKey, getUsersGetUserQueryKey, getUsersSearchUsersQueryKey, getUsersGetProfileQueryKey, getUsersGetCultureQueryKey, getUsersListWebAuthnCredentialsQueryKey, usersSearchUsers } from "./api/generated/users/users";
export { UsersCommandErrorKind } from "./api/generated/users/model/usersCommandErrorKind";
export type { UserSummaryResponse } from "./auth/userTypes";

export const tenantRoleSuffixes = {
  owner: "TenantOwner",
  user: "TenantUser",
} as const;

export type TenantRoleSuffix = (typeof tenantRoleSuffixes)[keyof typeof tenantRoleSuffixes];

/** Platform-wide role names the Users API returns in a user's global roles. */
export const globalRoleNames = {
  platformAdministrator: "PlatformAdmin",
} as const;
