namespace Vitarish.Sea.Domain.Dto;

public record AttachmentReadDto : EntityReadDto
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; } = default!;

    [JsonPropertyName("content")]
    public string Content { get; set; } = default!;

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = default!;

    [JsonPropertyName("contentThumbnail")]
    public string? ContentThumbnail { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("url")]
    public string Url => $"/sea/attachments/{Id}";
}

