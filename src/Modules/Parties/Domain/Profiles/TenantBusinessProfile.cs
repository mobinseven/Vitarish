namespace Vitarish.Parties.Domain.Profiles;

/// <summary>
/// A tenant's business category and public listing details. Kept apart from the tenant row,
/// which administration replaces as a whole, so those edits never erase what the owner shared.
/// </summary>
public sealed class TenantBusinessProfile
{
    public const int CategoryMaximumLength = 50;

    public Guid TenantId { get; set; }

    /// <summary>A category code from the tenant app's own list.</summary>
    public string? Category { get; set; }

    public TenantBusinessDetails? Details { get; set; }

    /// <summary>When the owner dismissed the prompt to add a logo; null while it shows.</summary>
    public DateTime? LogoPromptDismissedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
