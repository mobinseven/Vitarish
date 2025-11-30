using Microsoft.Extensions.Logging;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Contracts;
using Vorn.Aaas.Client.Api.Models;

namespace Vorn.Aaas.Client.Api.Internal;

internal sealed class RequestIdentityContext(
    IRequestIdentityForwarder forwarder) : IRequestIdentityContext, IRequestIdentityContextWriter
{
    private bool _isForwarding;

    public string Authority { get; private set; } = string.Empty;
    public string ClientId { get; private set; } = string.Empty;
    public string OwnerId { get; private set; } = string.Empty;
    public string? UserId { get; private set; }
    public string? TenantId { get; private set; }
    public string? ResourceId { get; private set; }
    public string? TraceId { get; private set; }
    public string? RequestId { get; private set; }
    public string? AdditionalInfo { get; private set; }

    public async Task Write(RequestIdentityContextCommand command)
    {
        Authority = command.Authority;
        ClientId = command.ClientId;
        OwnerId = command.OwnerId;
        TenantId = command.TenantId;
        UserId = command.UserId;
        ResourceId = command.ResourceId;
        TraceId = command.TraceId;
        RequestId = command.RequestId;
        AdditionalInfo = command.AdditionalInfo;

        if (!_isForwarding)
        {
            await Forward();
        }
    }

    private async Task Forward()
    {
        string currentRequestId = RequestId!;
        RequestIdentityDto envelope = new(
            currentRequestId,
            Authority,
            ClientId,
            OwnerId,
            TenantId,
            UserId,
            ResourceId,
            TraceId,
            AdditionalInfo,
            DateTimeOffset.UtcNow);

        _isForwarding = true;
        await forwarder.ForwardAsync(envelope, this);
        _isForwarding = false;
    }
}
