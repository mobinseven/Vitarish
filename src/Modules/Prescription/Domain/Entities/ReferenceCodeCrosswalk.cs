namespace Vitarish.Prescription.Domain.Entities;

/// <summary>
/// Maps between local/national codes and provider-specific codes.
/// </summary>
public sealed class ReferenceCodeCrosswalk
{
    public Guid Id { get; set; }
    public required string LocalCode { get; set; }
    public required string LocalCodeSystem { get; set; }
    public required string ProviderKey { get; set; }
    public required string ProviderCode { get; set; }
    public required string ProviderCodeSystem { get; set; }

    /// <summary>Confidence of the mapping (0.0 to 1.0).</summary>
    public decimal MappingConfidence { get; set; } = 1.0m;

    public bool IsActive { get; set; } = true;
    public DateTimeOffset? EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}
