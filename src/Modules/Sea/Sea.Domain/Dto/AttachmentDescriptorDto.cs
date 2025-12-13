namespace Vitarish.Sea.Domain.Dto;

public record AttachmentDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("eventId")]
    public Guid? EventId { get; init; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; init; }

    [JsonPropertyName("contentThumbnail")]
    public string? ContentThumbnail { get; init; }
}

