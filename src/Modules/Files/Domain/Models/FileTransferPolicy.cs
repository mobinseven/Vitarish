namespace Vitarish.Files.Domain.Models;

/// <summary>
/// The Files module's current byte-transfer ceiling. Callers cannot choose it.
/// </summary>
public static class FileTransferPolicy
{
    public const long MaximumContentBytes = 64L * 1024L * 1024L;
    public const long MaximumMultipartRequestBytes = MaximumContentBytes + 128L * 1024L;
    public const int MaximumMultipartFieldNameLength = 256;
    public const int MaximumMultipartFieldValueLength = 128 * 1024;
    public const int MaximumMultipartValueCount = 16;
    public const int TransferStartsPerMinute = 10;
    public static readonly TimeSpan UnclaimedUploadLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    private static readonly IReadOnlySet<string> AttachmentContentTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "application/pdf", "image/png", "image/jpeg", "image/webp", "text/plain", "text/markdown",
        "text/csv", "application/json", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "video/webm", "audio/wav",
        "audio/mpeg", "video/mp4"
    };

    private static readonly IReadOnlySet<string> KnowledgeContentTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "application/pdf", "text/plain", "text/markdown", "text/csv", "application/json",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    private static readonly IReadOnlySet<string> AvatarContentTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "image/png", "image/jpeg", "image/webp"
    };

    private static readonly IReadOnlySet<string> AudioContentTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "video/webm", "audio/wav", "audio/mpeg", "video/mp4"
    };

    public static FileTransferPolicyDefinition For(FileTransferPurpose purpose) => purpose switch
    {
        FileTransferPurpose.Attachment => new(purpose, MaximumContentBytes, AttachmentContentTypes),
        FileTransferPurpose.KnowledgeImport => new(purpose, MaximumContentBytes, KnowledgeContentTypes),
        FileTransferPurpose.Avatar => new(purpose, MaximumContentBytes, AvatarContentTypes),
        FileTransferPurpose.AudioRecording => new(purpose, MaximumContentBytes, AudioContentTypes),
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null)
    };

    public static bool IsAllowed(FileTransferPurpose purpose, string? contentType) =>
        TryNormalizeContentType(contentType, out string normalizedContentType) &&
        For(purpose).AllowedContentTypes.Contains(normalizedContentType);

    public static bool TryNormalizeContentType(string? value, out string contentType)
    {
        contentType = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            System.Net.Mime.ContentType parsed = new(value);
            if (string.IsNullOrWhiteSpace(parsed.MediaType))
            {
                return false;
            }

            contentType = parsed.MediaType.Trim().ToLowerInvariant();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
