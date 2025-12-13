namespace Vitarish.Sea.Domain.Models;

[EntityDescriptorFor(typeof(UserRole))]
public record UserRoleDescriptor : EntityDescriptor
{
    public Guid? UserId { get; init; }
    public Guid? RoleId { get; init; }
    public RoleType? Type { get; init; }
}

