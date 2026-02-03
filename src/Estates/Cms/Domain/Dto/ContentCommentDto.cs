namespace Vitarish.Cms.Domain.Dto;

public record ContentCommentDto(
    [property: JsonPropertyName("contentId")] Guid ContentId,
    [property: JsonPropertyName("parentCommentId")] Guid? ParentCommentId,
    [property: JsonPropertyName("authorName")] string AuthorName,
    [property: JsonPropertyName("authorId")] string? AuthorId,
    [property: JsonPropertyName("body")] string Body,
    [property: JsonPropertyName("isApproved")] bool IsApproved) : EntityDto;
