namespace Vitarish.Users.Application.Authorization;

/// <summary>
/// Owns the role-scope separation rule for role-management commands: an
/// account holds either platform roles or clinic (tenant) roles, never both.
/// Several clinic memberships, one role each, remain allowed. The rule guards
/// granting only; existing persisted role sets are not rewritten.
/// </summary>
public static class RoleScopeSeparationPolicy
{
    public static bool CanGrantGlobalRole(IEnumerable<string> assignedRoleNames) =>
        !assignedRoleNames.Any(TenantRoleDefinitions.IsTenantRoleName);

    public static bool CanGrantTenantRole(IEnumerable<string> assignedRoleNames) =>
        !assignedRoleNames.Any(TenantRoleDefinitions.PermittedGlobalRoles.Contains);
}
