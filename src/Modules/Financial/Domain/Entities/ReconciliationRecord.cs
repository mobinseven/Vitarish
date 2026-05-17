namespace Vitarish.Financial.Domain.Entities;

public sealed class ReconciliationRecord : Entity
{
    public required string ExternalReference { get; set; }

    public required string Provider { get; set; }

    public ReconciliationAuthority Authority { get; set; }

    public ReconciliationMismatchState MismatchState { get; set; }

    public Guid? RelatedEntryId { get; set; }

    public long? InternalMinorUnits { get; set; }

    public long? ExternalMinorUnits { get; set; }

    public required string Currency { get; set; }

    public int Exponent { get; set; }
}
