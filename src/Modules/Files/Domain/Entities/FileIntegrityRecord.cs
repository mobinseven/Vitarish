using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Domain.Entities;

public sealed class FileIntegrityRecord : Entity
{
    public required Guid FileInformationId { get; init; }
    public required string TenantId { get; init; }
    public required string ObjectKey { get; init; }
    public required string Digest { get; init; }
    public required string ContentType { get; init; }
    public required long ContentLength { get; init; }
    public int ProjectionVersion { get; init; } = 1;
    public FileIntegrityState State { get; private set; } = FileIntegrityState.Pending;
    public DateTime? VerifiedAt { get; private set; }

    public void MarkVerified(DateTime verifiedAt)
    {
        State = FileIntegrityState.Verified;
        VerifiedAt = verifiedAt;
    }

    public void MarkUnavailable()
    {
        State = FileIntegrityState.Unavailable;
        VerifiedAt = null;
    }
}
