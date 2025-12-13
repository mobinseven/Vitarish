namespace Vitarish.Sea.Domain.Models;

[EntityDescriptorFor(typeof(EventRole))]
public record EventRoleDescriptor : EntityDescriptor
{
    public Guid? EventId { get; init; }
    public Guid? RoleId { get; init; }
    public RoleType? Type { get; init; }
}
