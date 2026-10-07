namespace Vitarish.Files.Domain.Models;

/// <summary>
/// The leading-byte structure that identifies a file format regardless of its name or declared type.
/// </summary>
public enum FileSignature
{
    Jpeg,
    Png,
    WebP,
    HeifImage,
    Mp4,
    QuickTime,
    WebM,
    Pdf,
    OfficeOpenXml
}

/// <summary>
/// One format the Files module accepts for new uploads: its media type, the file extensions
/// that may carry it, and the signature its content must start with.
/// </summary>
public sealed record FileUploadFormat(string MediaType, IReadOnlyList<string> Extensions, FileSignature Signature);
