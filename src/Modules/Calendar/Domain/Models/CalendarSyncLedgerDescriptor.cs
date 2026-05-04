namespace Vitarish.Calendar.Domain.Models;

[EntityDescriptorFor(typeof(CalendarSyncLedger))]
public record CalendarSyncLedgerDescriptor : EntityDescriptor
{
    public Guid? CalendarAccountId { get; init; }
    public SyncStatus? Status { get; init; }
}
