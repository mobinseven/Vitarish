using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Main.Mock.Services;

public sealed class MockRequestContext : IRequestContext, IRequestContextWriter
{
    public string Authority { get; private set; } = "mock-authority";

    public string ClientId { get; private set; } = "mock-client";

    public string OwnerId { get; private set; } = "mock-owner";

    public string? UserId { get; private set; }

    public string? TenantId { get; private set; }

    public string? ResourceId { get; private set; }

    public string? TraceId { get; private set; }

    public string? RequestId { get; private set; } = Guid.NewGuid().ToString();

    public string? AdditionalInfo { get; private set; }

    public Request GetRequest() => new()
    {
        Authority = Authority,
        ClientId = ClientId,
        OwnerId = OwnerId,
        TenantId = TenantId,
        UserId = UserId,
        ResourceId = ResourceId,
        TraceId = TraceId,
    };

    public void SetContext(
        string authority,
        string clientId,
        string ownerId,
        string? tenantId = null,
        string? userId = null,
        string? resourceId = null,
        string? traceId = null,
        string? requestId = null,
        string? additionalInfo = null)
    {
        Authority = authority;
        ClientId = clientId;
        OwnerId = ownerId;
        TenantId = tenantId;
        UserId = userId;
        ResourceId = resourceId;
        TraceId = traceId;
        RequestId = requestId ?? Guid.NewGuid().ToString();
        AdditionalInfo = additionalInfo;
    }
}
