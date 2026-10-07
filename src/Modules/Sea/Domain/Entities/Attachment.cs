namespace Vitarish.Sea.Domain.Entities;

using Vitarish.Entities.Contracts.Models;
using Vitarish.Sea.Domain.Enums;

public class Attachment : EventItem
{
    public const int MaximumFileNameLength = 255;

    public static bool IsFileNameValid(string? value) => value is null ||
        (value.Length <= MaximumFileNameLength && !value.Any(char.IsControl) && !value.Contains('/') && !value.Contains('\\'));

    public static EntityResult<Attachment> Create(
        Guid eventId,
        string content,
        string contentType,
        string? contentThumbnail,
        long size,
        AttachmentKind kind,
        string? fileName)
    {
        EntityResult validation = ValidateNew(kind, fileName);
        return validation.IsFailure
            ? EntityResult<Attachment>.Failure([.. validation.Errors])
            : EntityResult<Attachment>.Success(new Attachment
            {
                EventId = eventId,
                Content = content,
                ContentType = contentType,
                ContentThumbnail = contentThumbnail,
                Size = size,
                Kind = kind,
                FileName = fileName
            });
    }

    /// <summary>
    /// New file content goes through the Files upload, which enforces the accepted formats and size; a file
    /// attachment keeps only the uploaded file's identifier. Existing inline content stays readable, so this
    /// applies to content being written, not to rebuilding stored attachments.
    /// </summary>
    public static EntityResult ValidateNewContent(AttachmentKind kind, string? content) =>
        kind == AttachmentKind.File && !Guid.TryParse(content, out _)
            ? EntityResult.Failure(EntityError.Validation("A file attachment must reference an uploaded file.", nameof(Content)))
            : EntityResult.Success();

    public static EntityResult ValidateNew(AttachmentKind kind, string? fileName)
    {
        if (!IsFileNameValid(fileName))
        {
            return EntityResult.Failure(EntityError.Validation(
                $"Attachment filename must be at most {MaximumFileNameLength} characters and contain no control characters or path separators.",
                nameof(FileName)));
        }

        if (kind == AttachmentKind.File && string.IsNullOrWhiteSpace(fileName))
        {
            return EntityResult.Failure(EntityError.Validation("A file attachment requires a filename.", nameof(FileName)));
        }

        if (kind == AttachmentKind.Text && fileName is not null)
        {
            return EntityResult.Failure(EntityError.Validation("A text attachment must not have a filename.", nameof(FileName)));
        }

        return EntityResult.Success();
    }

    public AttachmentKind Kind
    {
        get;
        set => SetField(ref field, value, nameof(Kind));
    }

    public string Content
    {
        get;
        set => SetField(ref field, value, nameof(Content));
    } = string.Empty;

    public string ContentType
    {
        get;
        set => SetField(ref field, value, nameof(ContentType));
    } = string.Empty;

    public string? ContentThumbnail
    {
        get;
        set => SetField(ref field, value, nameof(ContentThumbnail));
    }

    public long Size
    {
        get;
        set => SetField(ref field, value, nameof(Size));
    }

    public string? FileName
    {
        get;
        set
        {
            if (!IsFileNameValid(value))
            {
                throw new ArgumentException("Attachment filename must be display metadata without control characters or path separators.", nameof(value));
            }

            SetField(ref field, value, nameof(FileName));
        }
    }
}
