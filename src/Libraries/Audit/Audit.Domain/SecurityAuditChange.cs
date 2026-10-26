using System.Text.Json.Serialization;

namespace Vitarish.Audit.Domain;

/// <summary>
/// One catalogued field of an audited record, with its value before and after the change.
/// A null value means the field had no value on that side (for example, before creation).
/// </summary>
public sealed record SecurityAuditChange(SecurityAuditChangeField Field, string? PreviousValue, string? NewValue);

/// <summary>
/// The fields whose previous and new values a security event may carry. Writers reject any other field,
/// so credentials, tokens, and payloads cannot enter the audit through this channel.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SecurityAuditChangeField>))]
public enum SecurityAuditChangeField
{
    [JsonStringEnumMemberName("email")]
    Email,

    [JsonStringEnumMemberName("phoneNumber")]
    PhoneNumber,

    [JsonStringEnumMemberName("firstName")]
    FirstName,

    [JsonStringEnumMemberName("lastName")]
    LastName,

    [JsonStringEnumMemberName("accountStatus")]
    AccountStatus,

    [JsonStringEnumMemberName("globalRole")]
    GlobalRole,

    [JsonStringEnumMemberName("tenantRole")]
    TenantRole
}

public static class SecurityAuditChangeFields
{
    public const int MaximumChanges = 8;
    public const int MaximumValueLength = 256;

    public const SecurityAuditChangeField Email = SecurityAuditChangeField.Email;
    public const SecurityAuditChangeField PhoneNumber = SecurityAuditChangeField.PhoneNumber;
    public const SecurityAuditChangeField FirstName = SecurityAuditChangeField.FirstName;
    public const SecurityAuditChangeField LastName = SecurityAuditChangeField.LastName;
    public const SecurityAuditChangeField AccountStatus = SecurityAuditChangeField.AccountStatus;
    public const SecurityAuditChangeField GlobalRole = SecurityAuditChangeField.GlobalRole;
    public const SecurityAuditChangeField TenantRole = SecurityAuditChangeField.TenantRole;

    public const string AccountStatusEnabled = "enabled";
    public const string AccountStatusDisabled = "disabled";

    public static IReadOnlySet<SecurityAuditChangeField> All { get; } = new HashSet<SecurityAuditChangeField>
    {
        Email, PhoneNumber, FirstName, LastName, AccountStatus, GlobalRole, TenantRole
    };

    /// <summary>Whether the changes are catalogued, bounded, and free of control characters.</summary>
    public static bool AreValid(IReadOnlyList<SecurityAuditChange> changes) =>
        changes.Count <= MaximumChanges &&
        changes.Select(change => change.Field).Distinct().Count() == changes.Count &&
        changes.All(change => All.Contains(change.Field) && IsValidValue(change.PreviousValue) && IsValidValue(change.NewValue));

    private static bool IsValidValue(string? value) =>
        value is null || (value.Length <= MaximumValueLength && !value.Any(char.IsControl));
}
