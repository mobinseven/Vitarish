using Microsoft.Extensions.Logging;
using Vorn.Aaas.Client.Api.Abstractions;

namespace Vorn.Aaas.Client.Api.Internal;

internal sealed class RequestIdentityContext(
    IRequestIdentityForwarder forwarder,
    ILogger<RequestIdentityContext> logger) : IRequestIdentityContext, IRequestIdentityContextWriter
{
    public string Authority { get; private set; } = string.Empty;
    public string ClientId { get; private set; } = string.Empty;
    public string OwnerId { get; private set; } = string.Empty;
    public string? UserId { get; private set; }
    public string? TenantId { get; private set; }
    public string? ResourceId { get; private set; }
    public string? TraceId { get; private set; }
    public string? RequestId { get; private set; }
    public string? AdditionalInfo { get; private set; }

    public void Write(RequestIdentityContextCommand command)
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

        Forward();
    }

    private void Forward()
    {
        if (string.IsNullOrWhiteSpace(Authority) ||
            string.IsNullOrWhiteSpace(ClientId) ||
            string.IsNullOrWhiteSpace(OwnerId) ||
            string.IsNullOrWhiteSpace(RequestId))
        {
            return;
        }

        string currentRequestId = RequestId!;
        RequestIdentityEnvelope envelope = new(
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

        Task task = forwarder.ForwardAsync(envelope).AsTask();
        _ = task.ContinueWith(static (completed, state) =>
        {
            if (completed.IsFaulted && state is (ILogger<RequestIdentityContext> log, string body)) // state object contains resulting body and it might be the whole html page of error page
            {
                log.LogWarning(completed.Exception, "Failed forwarding request identity");
            }
        }, (logger, currentRequestId), TaskScheduler.Default);
    }
}
