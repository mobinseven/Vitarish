namespace Vitarish.Sea.Domain.Dto;

public record AttachmentWriteDto(
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("contentThumbnail")] string? ContentThumbnail,
    [property: JsonPropertyName("size")] long Size) : EntityWriteDto;
