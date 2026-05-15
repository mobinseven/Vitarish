namespace Vitarish.Contents.Domain.Dto;

public record ContentTermDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("contentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ContentId { get; set; }

    [JsonPropertyName("termId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? TermId { get; set; }

    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentTermRole? Role { get; set; }
}
