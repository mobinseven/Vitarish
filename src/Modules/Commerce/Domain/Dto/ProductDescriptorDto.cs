namespace Vitarish.Commerce.Domain.Dto;

public record ProductDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProductStatus? Status { get; set; }

    [JsonPropertyName("categoryId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CategoryId { get; set; }
}
