using Vitarish.Sea.Domain.Models;

namespace Vitarish.Sea.Domain.Entities;

public sealed class AttachmentIntegrityRecord : Entity
{
    public required Guid AttachmentId { get; init; }
    public required string TenantId { get; init; }
    public string Digest { get; private set; } = string.Empty;
    public int ProjectionVersion { get; init; } = AttachmentIntegrityProjection.Version;
    public AttachmentIntegrityState State { get; private set; } = AttachmentIntegrityState.Pending;
    public DateTime? VerifiedAt { get; private set; }

    public static AttachmentIntegrityRecord Create(Guid attachmentId, string tenantId, string digest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(digest);
        return new AttachmentIntegrityRecord
        {
            AttachmentId = attachmentId,
            TenantId = tenantId,
            Digest = digest
        };
    }

    public void MarkVerified(DateTime verifiedAt)
    {
        State = AttachmentIntegrityState.Verified;
        VerifiedAt = verifiedAt;
    }

    public void ReplaceDigest(string digest, DateTime verifiedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(digest);
        Digest = digest;
        MarkVerified(verifiedAt);
    }

    public void MarkUnavailable()
    {
        State = AttachmentIntegrityState.Unavailable;
        VerifiedAt = null;
    }
}
