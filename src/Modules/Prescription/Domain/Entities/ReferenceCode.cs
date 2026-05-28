namespace Vitarish.Prescription.Domain.Entities;

/// <summary>
/// Provider-specific reference code entry (drug, service, etc.).
/// Versioned by provider and code system.
/// </summary>
public sealed class ReferenceCode
{
    public Guid Id { get; set; }

    /// <summary>Provider key scoping this code.</summary>
    public required string ProviderKey { get; set; }

    /// <summary>Code system this code belongs to (e.g., "ProviderDrugCode", "NationalServiceCode").</summary>
    public required string CodeSystem { get; set; }

    /// <summary>The code value.</summary>
    public required string Code { get; set; }

    /// <summary>Human-readable display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Item category this code belongs to.</summary>
    public string? Category { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Version identifier for this code entry.</summary>
    public string? Version { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }

    /// <summary>Origin of this record (sync, manual, import).</summary>
    public string? RecordOrigin { get; set; }

    /// <summary>Checksum for change detection during sync.</summary>
    public string? Checksum { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
