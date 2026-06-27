using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record CategoryDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("parentCategoryId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ParentCategoryId { get; set; }
}
