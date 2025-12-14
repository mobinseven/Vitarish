namespace Vitarish.Clients.Modules.Sea.Dto;

public record AttachmentDto(
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("contentThumbnail")] string? ContentThumbnail,
    [property: JsonPropertyName("size")] long Size) : EntityDto
{
    [JsonPropertyName("url")]
    public string Url => $"/sea/attachments/{Id}";
}

