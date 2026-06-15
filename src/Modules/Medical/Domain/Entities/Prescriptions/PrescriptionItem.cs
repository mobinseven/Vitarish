using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Domain.Entities.Prescriptions;

/// <summary>
/// A single item within a prescription (drug, service, referral, etc.).
/// </summary>
public sealed class PrescriptionItem : Entity
{
    public Guid PrescriptionRecordId { get; set; }
    public PrescriptionItemCategory Category { get; set; }

    /// <summary>Reference code from provider's code system.</summary>
    public string? ReferenceCode { get; set; }

    /// <summary>Display name for the item.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Dosage/quantity/duration information (JSON or structured).</summary>
    public string? DosageJson { get; set; }

    public int Quantity { get; set; } = 1;
    public int? DurationDays { get; set; }

    /// <summary>Provider-specific item identifier resolved at submission time.</summary>
    public string? ProviderItemIdentifier { get; set; }

    /// <summary>Additional item-specific metadata as JSON.</summary>
    public string? MetadataJson { get; set; }

    public int SortOrder { get; set; }
}
