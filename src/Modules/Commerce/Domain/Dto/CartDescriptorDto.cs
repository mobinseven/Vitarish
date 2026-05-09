namespace Vitarish.Commerce.Domain.Dto;

public record CartDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("customerId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CustomerId { get; set; }

    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CartStatus? Status { get; set; }
}
