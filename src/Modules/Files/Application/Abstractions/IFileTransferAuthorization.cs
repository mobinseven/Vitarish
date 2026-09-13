namespace Vitarish.Files.Application.Abstractions;

/// <summary>
/// Application-owned authorization boundary for direct Files transfers.
/// </summary>
public interface IFileTransferAuthorization
{
    Task<bool> CanUploadAsync(CancellationToken cancellationToken = default);
}
