namespace Vitarish.Sea.Domain.Models;

[EntityDescriptorFor(typeof(Receipt))]
public record ReceiptDescriptor : EntityDescriptor
{
    public Guid? ProjectId { get; init; }
    public Guid? ProcedureId { get; init; }
    public Guid? EventId { get; init; }
    public Guid? ObjectId { get; init; }
}

