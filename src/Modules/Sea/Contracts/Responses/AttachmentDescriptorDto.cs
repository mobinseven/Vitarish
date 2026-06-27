using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Contracts.Responses;

public record AttachmentDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("eventId")]
    public Guid? EventId { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; init; }

    [JsonPropertyName("contentThumbnail")]
    public string? ContentThumbnail { get; init; }
}

