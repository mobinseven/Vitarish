using Vitarish.Files.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Files.Application.Access;

/// <summary>
/// Authorizes direct Files upload using trusted server identity and tenant context.
/// </summary>
public sealed class FileTransferAuthorization(IRequestContext requestContext) : IFileTransferAuthorization
{
    public Task<bool> CanUploadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            !string.IsNullOrWhiteSpace(requestContext.TenantId) &&
            !string.IsNullOrWhiteSpace(requestContext.UserId));
    }
}
