namespace Vitarish.Users.Application.Authorization;

public static class TenantRoleDefinitions
{
    public const string RoleSeparator = ":";

    public static IReadOnlySet<string> PermittedRoleSuffixes { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            IdentityDefinitions.TenantOwner,
            IdentityDefinitions.TenantUser
        };

    public static IReadOnlySet<string> PermittedGlobalRoles { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            IdentityDefinitions.PlatformAdmin
        };

    public static bool IsPermittedRoleSuffix(string? roleSuffix) =>
        roleSuffix is not null && PermittedRoleSuffixes.Contains(roleSuffix);

    public static bool IsTenantRoleSuffix(string? roleName) =>
        roleName is not null && PermittedRoleSuffixes.Contains(roleName);

    public static bool IsTenantRoleName(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return false;

        string[] parts = roleName.Split(RoleSeparator, 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 && Guid.TryParse(parts[0], out _) && IsPermittedRoleSuffix(parts[1]);
    }

    public static string BuildRoleName(Guid tenantId, string roleSuffix)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("A tenant identifier is required.", nameof(tenantId));
        if (!IsPermittedRoleSuffix(roleSuffix))
            throw new ArgumentException("The tenant role is not permitted.", nameof(roleSuffix));

        return $"{tenantId}{RoleSeparator}{roleSuffix}";
    }
}
