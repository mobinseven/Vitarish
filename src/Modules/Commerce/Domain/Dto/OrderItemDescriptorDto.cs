using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record OrderItemDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("orderId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? OrderId { get; set; }
}
