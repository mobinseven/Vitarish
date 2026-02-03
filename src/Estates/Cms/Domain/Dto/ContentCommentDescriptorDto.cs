namespace Vitarish.Cms.Domain.Dto;

public record ContentCommentDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("contentId")]
    public Guid? ContentId { get; init; }

    [JsonPropertyName("isApproved")]
    public bool? IsApproved { get; init; }
}
