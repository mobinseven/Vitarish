using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record CustomerProfileDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("userId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserId { get; set; }
}
