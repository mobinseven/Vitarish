using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Contracts.Responses;

public record AttachmentWriteDto(
    [property: JsonPropertyName("eventId")] Guid EventId,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("contentThumbnail")] string? ContentThumbnail,
    [property: JsonPropertyName("size")] long Size) : EntityWriteDto;
