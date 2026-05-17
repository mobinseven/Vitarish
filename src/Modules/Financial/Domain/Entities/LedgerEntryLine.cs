namespace Vitarish.Financial.Domain.Entities;

public sealed class LedgerEntryLine : Entity
{
    public Guid LedgerEntryId { get; set; }

    public Guid AccountId { get; set; }

    public LedgerEntrySide Side { get; set; }

    public long AmountMinorUnits { get; set; }
}
