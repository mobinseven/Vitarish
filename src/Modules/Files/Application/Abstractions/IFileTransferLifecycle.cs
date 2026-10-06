using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Application.Abstractions;

/// <summary>
/// Files-owned lifecycle capability. Implementations persist transfer and reference state.
/// </summary>
public interface IFileTransferLifecycle
{
    Task<FileUploadPreparation> PrepareAsync(
        FileTransferPurpose purpose,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    Task<FileReference> ClaimAsync(Guid fileId, Guid transferId, CancellationToken cancellationToken = default);

    Task<FileReference> ReleaseAsync(Guid fileId, Guid transferId, CancellationToken cancellationToken = default);
}
