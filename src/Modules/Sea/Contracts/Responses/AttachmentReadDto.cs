using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Contracts.Responses;

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

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("kind")]
    public Enums.AttachmentKind Kind { get; set; }

}
