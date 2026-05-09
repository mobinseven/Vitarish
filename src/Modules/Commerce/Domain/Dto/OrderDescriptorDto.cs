namespace Vitarish.Commerce.Domain.Dto;

public record OrderDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("customerId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CustomerId { get; set; }

    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrderStatus? Status { get; set; }
}
