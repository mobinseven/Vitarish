using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Application.Abstractions;

/// <summary>
/// Registered by composition for a feature that owns the resource relationship.
/// </summary>
public interface IFileResourceAuthorization
{
    Task<bool> CanTransferAsync(
        FileTransferPurpose purpose,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);
}
