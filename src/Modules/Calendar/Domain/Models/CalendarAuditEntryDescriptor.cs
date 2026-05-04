namespace Vitarish.Calendar.Domain.Models;

[EntityDescriptorFor(typeof(CalendarAuditEntry))]
public record CalendarAuditEntryDescriptor : EntityDescriptor
{
    public Guid? ObjectMappingId { get; init; }
    public Guid? AccountId { get; init; }
    public CalendarAuditAction? Action { get; init; }
}
