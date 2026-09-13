namespace Vitarish.Sea.Domain.Models;

public static class AttachmentIntegrityAuditContract
{
    public const string ResourcePrefix = "sea/attachment/";
    public const string SourceBoundary = "sea.attachment-integrity";
    public const string IdempotencySuffix = ":sea-attachment-integrity";
}
