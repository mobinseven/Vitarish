namespace Vitarish.Cms.Domain.Dto;

public record ContentWriteDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("bodyHtml")] string BodyHtml,
    [property: JsonPropertyName("summary")] string? Summary,
    [property: JsonPropertyName("authorId")] string? AuthorId,
    [property: JsonPropertyName("status")] ContentStatus Status,
    [property: JsonPropertyName("publishedAt")] DateTimeOffset? PublishedAt) : EntityWriteDto;
