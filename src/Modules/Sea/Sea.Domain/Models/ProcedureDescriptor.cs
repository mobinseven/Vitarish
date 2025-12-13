namespace Vitarish.Sea.Domain.Models;

[EntityDescriptorFor(typeof(Procedure))]
public record ProcedureDescriptor : EntityDescriptor
{
    public Guid? ProjectId { get; init; }
    public IReadOnlyList<Guid>? RoleIds { get; init; }
    public bool IncludeStats { get; init; } = true;
}

