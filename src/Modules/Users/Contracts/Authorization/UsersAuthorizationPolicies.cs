namespace Vitarish.Users.Contracts.Authorization;

public static class UsersAuthorizationPolicies
{
    public const string ManageAccounts = "Users.SecurityAccounts.Manage";
    public const string ReadSecurityState = "Users.SecurityState.Read";
    public const string AssignGlobalRole = "Users.GlobalRole.Assign";
    public const string ReadTenantRoles = "Users.TenantRoles.Read";
    public const string ReplaceTenantRole = "Users.TenantRole.Replace";
    public const string ManageTenantMembers = "Users.TenantMembers.Manage";
}
