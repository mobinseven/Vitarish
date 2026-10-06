using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Domain.Entities;

public sealed class FileTransferRecord : Entity
{
    public required string TenantId { get; init; }
    public required FileTransferPurpose Purpose { get; init; }
    public required string ResourceType { get; init; }
    public required string ResourceId { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public FileTransferState State { get; private set; } = FileTransferState.Prepared;

    public bool IsExpired(DateTime now) => ExpiresAt <= now;

    public void Consume() => State = FileTransferState.Consumed;

    public void Expire() => State = FileTransferState.Expired;
}
