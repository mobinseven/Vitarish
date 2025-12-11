using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;

namespace Vitarish.Clients.Identity.Contracts;

public sealed class SignalRClientRegistration
{
    public string? ClientId { get; set; }
    public string ResourceId { get; set; } = string.Empty;
    public bool AutomaticReconnect { get; set; } = true;
    public bool UseMessagePack { get; set; } = true;
    public Action<HubConnectionBuilder>? ConfigureBuilder { get; set; }
    public Action<HttpConnectionOptions>? ConfigureHttpConnection { get; set; }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ResourceId))
        {
            throw new InvalidOperationException("SignalR client registration requires a non-empty resource id.");
        }
    }
}
