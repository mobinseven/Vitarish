using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record CartItemDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("cartId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CartId { get; set; }
}
