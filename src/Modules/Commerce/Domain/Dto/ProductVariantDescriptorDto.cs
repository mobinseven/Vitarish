using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record ProductVariantDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("productId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ProductId { get; set; }
}
