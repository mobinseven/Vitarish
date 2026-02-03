namespace Vitarish.Cms.Domain.Dto;

public record ContentDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("status")]
    public ContentStatus? Status { get; init; }

    [JsonPropertyName("tagId")]
    public Guid? TagId { get; init; }

    [JsonPropertyName("categoryId")]
    public Guid? CategoryId { get; init; }
}
