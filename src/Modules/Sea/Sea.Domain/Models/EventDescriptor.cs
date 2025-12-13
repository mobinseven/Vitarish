namespace Vitarish.Sea.Domain.Models;

[EntityDescriptorFor(typeof(Event))]
public record EventDescriptor : EntityDescriptor
{
    public Guid? ProjectId { get; init; }
    public Guid? ProcedureId { get; init; }
    public Guid? PreviousEventId { get; init; }
    public Guid? CommittedBy { get; init; }
    public DateTimeOffset? CommittedFrom { get; init; }
    public DateTimeOffset? CommittedTo { get; init; }
    public bool? IsCommitted { get; init; }
    public Guid? ReadRoleId { get; init; }
    public Guid? WriteRoleId { get; init; }
    public bool IncludeStats { get; init; } = true;
}

