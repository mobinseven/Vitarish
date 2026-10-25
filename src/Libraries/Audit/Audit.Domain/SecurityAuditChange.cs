namespace Vitarish.Audit.Domain;

/// <summary>
/// One catalogued field of an audited record, with its value before and after the change.
/// A null value means the field had no value on that side (for example, before creation).
/// </summary>
public sealed record SecurityAuditChange(string Field, string? PreviousValue, string? NewValue);

/// <summary>
/// The fields whose previous and new values a security event may carry. Writers reject any other field,
/// so credentials, tokens, and payloads cannot enter the audit through this channel.
/// </summary>
public static class SecurityAuditChangeFields
{
    public const int MaximumChanges = 8;
    public const int MaximumValueLength = 256;

    public const string Email = "email";
    public const string PhoneNumber = "phoneNumber";
    public const string FirstName = "firstName";
    public const string LastName = "lastName";
    public const string AccountStatus = "accountStatus";
    public const string GlobalRole = "globalRole";
    public const string TenantRole = "tenantRole";

    public const string AccountStatusEnabled = "enabled";
    public const string AccountStatusDisabled = "disabled";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Email, PhoneNumber, FirstName, LastName, AccountStatus, GlobalRole, TenantRole
    };

    /// <summary>Whether the changes are catalogued, bounded, and free of control characters.</summary>
    public static bool AreValid(IReadOnlyList<SecurityAuditChange> changes) =>
        changes.Count <= MaximumChanges &&
        changes.Select(change => change.Field).Distinct(StringComparer.Ordinal).Count() == changes.Count &&
        changes.All(change => All.Contains(change.Field) && IsValidValue(change.PreviousValue) && IsValidValue(change.NewValue));

    private static bool IsValidValue(string? value) =>
        value is null || (value.Length <= MaximumValueLength && !value.Any(char.IsControl));
}
