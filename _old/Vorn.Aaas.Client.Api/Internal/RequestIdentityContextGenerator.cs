using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Contracts;
using Vorn.Aaas.Client.Api.Options;

namespace Vorn.Aaas.Client.Api.Internal;

internal sealed class RequestIdentityContextGenerator(
    IRequestIdentityContextWriter contextWriter,
    IOptions<VornOptions> options) : IRequestIdentityContextGenerator
{
    public async Task Generate(ScopeRequestIdentity? request = null)
    {
        VornOptions vorn = options.Value;

        string authority = vorn.Aaas.Authority ?? throw new InvalidOperationException("AaaS Authority missing.");
        string clientId = vorn.Aaas.ClientId ?? throw new InvalidOperationException("AAAS client id missing.");
        string resolvedOwnerId = request?.OwnerId ?? clientId;
        string resolvedTenantId = request?.TenantId ?? $"{clientId}.{resolvedOwnerId}";
        string resolvedRequestId = request?.RequestId ?? Guid.NewGuid().ToString();

        RequestIdentityContextCommand command = new(
            authority,
            clientId,
            resolvedOwnerId,
            resolvedTenantId,
            null,
            request?.ResourceId,
            Activity.Current?.TraceId.ToHexString(),
            resolvedRequestId,
            request?.AdditionalInfo);

        await contextWriter.Write(command);

    }
}
