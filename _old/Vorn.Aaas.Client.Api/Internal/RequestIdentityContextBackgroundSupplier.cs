namespace Vorn.Aaas.Client.Api.Internal;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Options;

internal sealed class RequestIdentityContextBackgroundSupplier(
    IRequestIdentityContextWriter contextWriter,
    IOptions<VornOptions> options,
    ILogger<RequestIdentityContextBackgroundSupplier> logger) : IRequestIdentityContextBackgroundSupplier
{
    public IDisposable? Supply(
        string? resourceId = null,
        string? ownerId = null,
        string? tenantId = null,
        string? requestId = null,
        string? additionalInfo = null)
    {
        VornOptions vorn = options.Value;

        string authority = vorn.Aaas.Authority ?? throw new InvalidOperationException("AaaS Authority missing.");
        string clientId = vorn.Aaas.ClientId ?? throw new InvalidOperationException("AAAS client id missing.");
        string resolvedOwnerId = ownerId ?? clientId;
        string resolvedTenantId = tenantId ?? $"{clientId}.{resolvedOwnerId}";
        string resolvedRequestId = requestId ?? Guid.NewGuid().ToString();

        IDictionary<string, object> scopeState = new Dictionary<string, object>
        {
            ["request_id"] = resolvedRequestId
        };
        IDisposable? scope = logger.BeginScope(scopeState);

        RequestIdentityContextCommand command = new(
            authority,
            clientId,
            resolvedOwnerId,
            resolvedTenantId,
            null,
            resourceId,
            Activity.Current?.TraceId.ToHexString(),
            resolvedRequestId,
            additionalInfo);

        contextWriter.Write(command);

        return scope;
    }
}
