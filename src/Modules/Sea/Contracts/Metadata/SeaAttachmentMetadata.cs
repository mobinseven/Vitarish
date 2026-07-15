namespace Vitarish.Sea.Contracts.Metadata;

public sealed record SeaAttachmentMetadata(Guid AttachmentId, Guid EventId, string? Content, string? ContentType, string? ContentThumbnail, long? Size, string? FileId, Enums.AttachmentKind Kind, string? FileName, DateTime CreatedAt, string? CreatedBy, DateTime? UpdatedAt, string? UpdatedBy);
