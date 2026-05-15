namespace Vitarish.Contents.Domain.Dto;

public record ContentRevisionDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("contentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ContentId { get; set; }

    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Version { get; set; }
}
