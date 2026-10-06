namespace Vitarish.Users.Application.Authorization;

/// <summary>
/// Owns the access-level rule: an account may sign in only while it holds at
/// least one permitted global role or one tenant membership role. Users owns
/// role assignment and membership naming, so this decision stays here and
/// Authentication consumes it through the existing role capability.
/// </summary>
public static class UserAccessLevelPolicy
{
    public static bool HasAccessLevel(IEnumerable<string?> roleNames) =>
        roleNames.Any(role => role is not null &&
            (TenantRoleDefinitions.PermittedGlobalRoles.Contains(role) || TenantRoleDefinitions.IsTenantRoleName(role)));
}
