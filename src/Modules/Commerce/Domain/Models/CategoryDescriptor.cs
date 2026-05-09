namespace Vitarish.Commerce.Domain.Models;

[EntityDescriptorFor(typeof(Category))]
public record CategoryDescriptor : EntityDescriptor
{
    public Guid? ParentCategoryId { get; init; }
}
