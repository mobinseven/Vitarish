using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Domain.Entities;

public sealed class FileReferenceRecord : Entity
{
    public required Guid FileInformationId { get; init; }
    public required Guid TransferId { get; init; }
    public required string TenantId { get; init; }
    public FileReferenceState State { get; private set; } = FileReferenceState.Reserved;

    public void Confirm() => State = FileReferenceState.Confirmed;

    public void Release() => State = FileReferenceState.Released;
}
