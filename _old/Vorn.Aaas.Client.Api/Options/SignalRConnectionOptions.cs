namespace Vorn.Aaas.Client.Api.Options;

using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;

public sealed record SignalRConnectionOptions
{
    public string? ClientId { get; init; }
    public string? ResourceId { get; init; }
    public string? ApiClientId { get; init; }
    public string? OwnerClientId { get; init; }
    public bool? AutomaticReconnect { get; init; }
    public bool? UseMessagePack { get; init; }
    public Action<HubConnectionBuilder>? ConfigureBuilder { get; init; }
    public Action<HttpConnectionOptions>? ConfigureHttpConnection { get; init; }
}
