namespace Vitarish.Parties.Domain.Profiles;

/// <summary>
/// A tenant's business category and public listing details. Kept apart from the tenant row,
/// which administration replaces as a whole, so those edits never erase what the owner shared.
/// </summary>
public sealed class TenantBusinessProfile
{
    public const int CategoryMaximumLength = 50;
    public const int TimeZoneIdMaximumLength = 128;
    public const string DefaultTimeZoneId = "Asia/Tehran";

    public Guid TenantId { get; set; }

    /// <summary>A category code from the tenant app's own list.</summary>
    public string? Category { get; set; }

    public TenantBusinessDetails? Details { get; set; }

    public static bool IsValidTimeZoneId(string value) =>
        value.Length is > 0 and <= TimeZoneIdMaximumLength
        && TimeZoneInfo.TryConvertIanaIdToWindowsId(value, out _)
        && TimeZoneInfo.TryFindSystemTimeZoneById(value, out _);

    public void SetTimeZoneId(string value)
    {
        if (!IsValidTimeZoneId(value))
            throw new ArgumentException("A supported IANA time zone is required.", nameof(value));

        Details = TenantBusinessDetails.WithTimeZone(Details, value);
    }

    public void ReplacePublicDetails(TenantBusinessDetails? details) =>
        Details = Details?.TimeZoneId is { } timeZoneId
            ? TenantBusinessDetails.WithTimeZone(details, timeZoneId)
            : details;

    /// <summary>When the owner dismissed the prompt to add a logo; null while it shows.</summary>
    public DateTime? LogoPromptDismissedAt { get; set; }

    /// <summary>When an owner or platform admin chose to set the clinic up later; null while the guide shows.</summary>
    public DateTime? SetupGuideDismissedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
