using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Authorization;

namespace Vitarish.Users.Application.Security;

/// <summary>
/// The previous and new contact and name values of an account for its audit record, listing only the
/// fields that differ. Values longer than the audit bound are shortened rather than dropped.
/// </summary>
public static class UserAuditChanges
{
    private const string GlobalRoleSeparator = ",";

    public static IReadOnlyList<SecurityAuditChange> Between(
        (string? Email, string? PhoneNumber, string? FirstName, string? LastName) previous,
        (string? Email, string? PhoneNumber, string? FirstName, string? LastName) current)
    {
        List<SecurityAuditChange> changes = [];
        Add(changes, SecurityAuditChangeFields.Email, previous.Email, current.Email, StringComparer.Ordinal);
        Add(changes, SecurityAuditChangeFields.PhoneNumber, previous.PhoneNumber, current.PhoneNumber, StringComparer.Ordinal);
        Add(changes, SecurityAuditChangeFields.FirstName, previous.FirstName, current.FirstName, StringComparer.Ordinal);
        Add(changes, SecurityAuditChangeFields.LastName, previous.LastName, current.LastName, StringComparer.Ordinal);
        return changes;
    }

    public static IReadOnlyList<SecurityAuditChange> GlobalRoles(IEnumerable<string> previous, IEnumerable<string> current)
    {
        string before = FormatGlobalRoles(previous);
        string after = FormatGlobalRoles(current);
        return string.Equals(before, after, StringComparison.Ordinal)
            ? []
            : [new SecurityAuditChange(SecurityAuditChangeFields.GlobalRole, before, after)];
    }

    private static string FormatGlobalRoles(IEnumerable<string> roleNames)
    {
        IReadOnlyList<string> roles = TenantRoleDefinitions.GlobalRoles(roleNames);
        return roles.Count == 0
            ? SecurityAuditReasonCatalog.TenantRoleStateNone
            : string.Join(GlobalRoleSeparator, roles);
    }

    private static void Add(List<SecurityAuditChange> changes, SecurityAuditChangeField field, string? previous, string? current, StringComparer comparer)
    {
        string? before = Normalize(previous);
        string? after = Normalize(current);
        if (!comparer.Equals(before ?? string.Empty, after ?? string.Empty))
            changes.Add(new SecurityAuditChange(field, Bound(before), Bound(after)));
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return new string(value.Where(character => !char.IsControl(character)).ToArray());
    }

    private static string? Bound(string? value) =>
        value is null || value.Length <= SecurityAuditChangeFields.MaximumValueLength
            ? value
            : value[..SecurityAuditChangeFields.MaximumValueLength];
}
