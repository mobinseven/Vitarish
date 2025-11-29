namespace Vorn.Aaas.Client.Api.Options;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections.Client;

public sealed class SignalRClientRegistration
{
    public string? ClientId { get; set; }
    public string ResourceId { get; set; } = string.Empty;
    public string? ApiClientId { get; set; }
    public string? OwnerClientId { get; set; }
    public bool AutomaticReconnect { get; set; } = true;
    public bool UseMessagePack { get; set; } = true;
    public Action<HubConnectionBuilder>? ConfigureBuilder { get; set; }
    public Action<HttpConnectionOptions>? ConfigureHttpConnection { get; set; }

    internal void Validate()
    {
        if(string.IsNullOrWhiteSpace(ClientId) && string.IsNullOrWhiteSpace(ApiClientId))
        {
            throw new InvalidOperationException("SignalR client registration requires a client id or an explicit API client id.");
        }

        if(string.IsNullOrWhiteSpace(ResourceId))
        {
            throw new InvalidOperationException("SignalR client registration requires a non-empty resource id.");
        }
    }
}
