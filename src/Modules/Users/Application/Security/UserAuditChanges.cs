using Vitarish.Audit.Domain;

namespace Vitarish.Users.Application.Security;

/// <summary>
/// The previous and new contact and name values of an account for its audit record, listing only the
/// fields that differ. Values longer than the audit bound are shortened rather than dropped.
/// </summary>
public static class UserAuditChanges
{
    public static IReadOnlyList<SecurityAuditChange> Between(
        (string? Email, string? PhoneNumber, string? FirstName, string? LastName) previous,
        (string? Email, string? PhoneNumber, string? FirstName, string? LastName) current)
    {
        List<SecurityAuditChange> changes = [];
        Add(changes, SecurityAuditChangeFields.Email, previous.Email, current.Email, StringComparer.OrdinalIgnoreCase);
        Add(changes, SecurityAuditChangeFields.PhoneNumber, previous.PhoneNumber, current.PhoneNumber, StringComparer.Ordinal);
        Add(changes, SecurityAuditChangeFields.FirstName, previous.FirstName, current.FirstName, StringComparer.Ordinal);
        Add(changes, SecurityAuditChangeFields.LastName, previous.LastName, current.LastName, StringComparer.Ordinal);
        return changes;
    }

    private static void Add(List<SecurityAuditChange> changes, string field, string? previous, string? current, StringComparer comparer)
    {
        string? before = Bound(previous);
        string? after = Bound(current);
        if (!comparer.Equals(before ?? string.Empty, after ?? string.Empty))
            changes.Add(new SecurityAuditChange(field, before, after));
    }

    private static string? Bound(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        string cleaned = new(value.Where(character => !char.IsControl(character)).ToArray());
        return cleaned.Length <= SecurityAuditChangeFields.MaximumValueLength
            ? cleaned
            : cleaned[..SecurityAuditChangeFields.MaximumValueLength];
    }
}
