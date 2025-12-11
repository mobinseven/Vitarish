using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Clients.Gateway.Abstractions;
using Vitarish.Clients.Gateway.Contracts;
using Vitarish.Clients.Gateway.Extensions;
using Vitarish.Clients.Gateway.Options;

namespace Vitarish.Clients.Gateway.Internal;

internal class SignalRClientFactory(
    IApiClientService apiClientService,
    IClientIdentityContext identityContext,
    IOptions<IdentityOptions> aaasOptions,
    IOptions<SignalRClientOptions> signalROptions) : ISignalRClientFactory
{
    private readonly SignalRClientOptions registrations = signalROptions.Value;
    private readonly IdentityOptions configuration = aaasOptions.Value;

    public Task<HubConnection> CreateConnection<TClient>(CancellationToken cancellationToken = default) =>
        CreateConnection<TClient>(default, cancellationToken);

    public Task<HubConnection> CreateConnection<TClient>(SignalRConnectionOptions? options, CancellationToken cancellationToken = default) =>
        CreateConnection(SignalRClientOptions.KeyFromType(typeof(TClient)), options, cancellationToken);

    public Task<HubConnection> CreateConnection(string name, CancellationToken cancellationToken = default) =>
        CreateConnection(name, default, cancellationToken);

    public Task<HubConnection> CreateConnection(string name, SignalRConnectionOptions? options, CancellationToken cancellationToken = default) =>
        registrations.TryGetRegistration(name, out SignalRClientRegistration? registration) && registration is not null
            ? BuildConnectionAsync(registration, options, cancellationToken)
            : throw new InvalidOperationException($"SignalR client '{name}' is not registered.");

    private async Task<HubConnection> BuildConnectionAsync(SignalRClientRegistration registration, SignalRConnectionOptions? options, CancellationToken cancellationToken)
    {
        SignalRClientRegistration merged = MergeOptions(registration, options);
        ApiClientDto apiClient = await ResolveApiClientAsync(merged, cancellationToken).ConfigureAwait(false);
        string hubUrl = BuildHubUrl(apiClient, merged);

        HubConnectionBuilder builder = new();
        _ = builder.WithUrl(hubUrl, httpOptions =>
        {
            httpOptions.AccessTokenProvider = async () => (string?)await GetAccessTokenAsync(apiClient).ConfigureAwait(false);
            merged.ConfigureHttpConnection?.Invoke(httpOptions);
        });

        if (merged.AutomaticReconnect)
        {
            _ = builder.WithAutomaticReconnect();
        }

        if (merged.UseMessagePack)
        {
            _ = builder.AddMessagePackProtocol();
        }

        merged.ConfigureBuilder?.Invoke(builder);
        return builder.Build();
    }

    private static SignalRClientRegistration MergeOptions(SignalRClientRegistration registration, SignalRConnectionOptions? options)
    {
        SignalRClientRegistration merged = new()
        {
            ClientId = options?.ClientId ?? registration.ClientId,
            ResourceId = options?.ResourceId ?? registration.ResourceId,
            ApiClientId = options?.ApiClientId ?? registration.ApiClientId,
            OwnerClientId = options?.OwnerClientId ?? registration.OwnerClientId,
            AutomaticReconnect = options?.AutomaticReconnect ?? registration.AutomaticReconnect,
            UseMessagePack = options?.UseMessagePack ?? registration.UseMessagePack,
            ConfigureBuilder = registration.ConfigureBuilder,
            ConfigureHttpConnection = registration.ConfigureHttpConnection,
        };

        if (options?.ConfigureBuilder is not null)
        {
            merged.ConfigureBuilder += options.ConfigureBuilder;
        }

        if (options?.ConfigureHttpConnection is not null)
        {
            merged.ConfigureHttpConnection += options.ConfigureHttpConnection;
        }

        merged.Validate();
        return merged;
    }

    private async Task<ApiClientDto> ResolveApiClientAsync(SignalRClientRegistration registration, CancellationToken cancellationToken)
    {
        string clientId = ResolveApiClientId(registration);
        ApiClientDto? client = await apiClientService.GetApiClient(clientId, cancellationToken).ConfigureAwait(false);
        return client ?? throw new InvalidOperationException($"No such API client registered: {clientId}");
    }

    private string ResolveApiClientId(SignalRClientRegistration registration) => !string.IsNullOrWhiteSpace(registration.ApiClientId)
            ? registration.ApiClientId
            : string.IsNullOrWhiteSpace(registration.ClientId)
            ? throw new InvalidOperationException("SignalR client registration requires either ClientId or ApiClientId to resolve the API client.")
            : DeriveClientId(registration.ClientId);

    private string DeriveClientId(string clientPath)
    {
        string? target = clientPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidOperationException($"Unable to derive API client id from client path '{clientPath}'.");
        }

        string suffix = configuration.Local ?? false ? ".local" : configuration.Dev ?? false ? ".dev" : string.Empty;
        return $"api.{target}{suffix}";
    }

    private string BuildHubUrl(ApiClientDto apiClient, SignalRClientRegistration registration)
    {
        string hostBase = $"https://{apiClient.HostUri}";
        string clientSegment = ResolveClientSegment(registration);
        string resourceSegment = ResolveResourceSegment(registration);
        string combinedPath = string.IsNullOrWhiteSpace(clientSegment)
            ? resourceSegment
            : $"{clientSegment}/{resourceSegment}";
        return UrlHelper.UrlCombine(hostBase, combinedPath);
    }

    private async Task<string> GetAccessTokenAsync(ApiClientDto apiClient)
    {
        string? token = await apiClientService.GetAccessTokenAsync(apiClient).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(token)
            ? throw new InvalidOperationException($"Unable to acquire access token for client '{apiClient.Id}'.")
            : token;
    }

    private string ResolveClientSegment(SignalRClientRegistration registration)
    {
        string? candidate = registration.ClientId ?? identityContext.ClientId;
        return string.IsNullOrWhiteSpace(candidate)
            ? throw new InvalidOperationException("SignalR client registration requires a client id to build the hub path.")
            : NormalizeSegment(candidate);
    }

    private string ResolveResourceSegment(SignalRClientRegistration registration)
    {
        string? candidate = registration.ResourceId ?? identityContext.ResourceId;
        return string.IsNullOrWhiteSpace(candidate)
            ? throw new InvalidOperationException("SignalR client registration requires a resource id to build the hub path.")
            : NormalizeSegment(candidate);
    }

    private static string NormalizeSegment(string path)
    {
        string normalized = string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new InvalidOperationException("SignalR client registration requires non-empty path segments.")
            : normalized;
    }
}
