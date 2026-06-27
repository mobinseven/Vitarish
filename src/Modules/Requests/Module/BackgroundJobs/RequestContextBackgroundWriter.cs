using Vitarish.Modules.Base.BackgroundJobs;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Requests.Module.BackgroundJobs;

/// <summary>
/// Adapts the requests context writer to the base background request context contract.
/// </summary>
public sealed class RequestContextBackgroundWriter(IRequestContextWriter requestContextWriter) : IBackgroundRequestContextWriter
{
    /// <inheritdoc />
    public void SetContext(BackgroundRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        requestContextWriter.SetContext(
            authority: context.Authority,
            clientId: context.ClientId,
            ownerId: context.OwnerId,
            tenantId: context.TenantId,
            resourceId: context.ResourceId,
            requestId: context.RequestId);
    }
}
