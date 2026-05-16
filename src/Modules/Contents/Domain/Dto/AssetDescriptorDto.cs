namespace Vitarish.Contents.Domain.Dto;

public record AssetDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AssetType? Type { get; set; }

    [JsonPropertyName("fileId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? FileId { get; set; }

    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }
}
