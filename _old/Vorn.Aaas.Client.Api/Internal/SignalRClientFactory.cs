using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Contracts;
using Vorn.Aaas.Client.Api.Extensions;
using Vorn.Aaas.Client.Api.Options;

namespace Vorn.Aaas.Client.Api.Internal;

internal class SignalRClientFactory(
    IApiClientService apiClientService,
    IRequestIdentityContext identityContext,
    IOptions<VornOptions> vornOptions,
    IOptions<SignalRClientOptions> signalROptions) : ISignalRClientFactory
{
    private readonly SignalRClientOptions registrations = signalROptions.Value;
    private readonly VornOptions configuration = vornOptions.Value;

    public Task<HubConnection> CreateConnection<TClient>(CancellationToken cancellationToken = default) =>
        CreateConnection<TClient>(default, cancellationToken);

    public Task<HubConnection> CreateConnection<TClient>(SignalRConnectionOptions? options, CancellationToken cancellationToken = default) =>
        CreateConnection(SignalRClientOptions.KeyFromType(typeof(TClient)), options, cancellationToken);

    public Task<HubConnection> CreateConnection(string name, CancellationToken cancellationToken = default) =>
        CreateConnection(name, default, cancellationToken);

    public Task<HubConnection> CreateConnection(string name, SignalRConnectionOptions? options, CancellationToken cancellationToken = default) =>
        registrations.TryGetRegistration(name, out SignalRClientRegistration? registration) && registration is not null
            ? buildConnectionAsync(registration, options, cancellationToken)
            : throw new InvalidOperationException($"SignalR client '{name}' is not registered.");

    private async Task<HubConnection> buildConnectionAsync(SignalRClientRegistration registration, SignalRConnectionOptions? options, CancellationToken cancellationToken)
    {
        SignalRClientRegistration merged = mergeOptions(registration, options);
        ApiClientDto apiClient = await resolveApiClientAsync(merged, cancellationToken).ConfigureAwait(false);
        string hubUrl = buildHubUrl(apiClient, merged);
        IReadOnlyDictionary<string, string> headers = buildHeaders();

        HubConnectionBuilder builder = new();
        _ = builder.WithUrl(hubUrl, httpOptions =>
        {
            httpOptions.AccessTokenProvider = async () => (string?)await getAccessTokenAsync(apiClient).ConfigureAwait(false);
            foreach ((string key, string value) in headers)
            {
                httpOptions.Headers[key] = value;
            }

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

    private static SignalRClientRegistration mergeOptions(SignalRClientRegistration registration, SignalRConnectionOptions? options)
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

    private async Task<ApiClientDto> resolveApiClientAsync(SignalRClientRegistration registration, CancellationToken cancellationToken)
    {
        string clientId = resolveApiClientId(registration);
        ApiClientDto? client = await apiClientService.GetApiClient(clientId, cancellationToken).ConfigureAwait(false);
        return client ?? throw new InvalidOperationException($"No such API client registered: {clientId}");
    }

    private string resolveApiClientId(SignalRClientRegistration registration) => !string.IsNullOrWhiteSpace(registration.ApiClientId)
            ? registration.ApiClientId
            : string.IsNullOrWhiteSpace(registration.ClientId)
            ? throw new InvalidOperationException("SignalR client registration requires either ClientId or ApiClientId to resolve the API client.")
            : deriveClientId(registration.ClientId);

    private string deriveClientId(string clientPath)
    {
        string? target = clientPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidOperationException($"Unable to derive API client id from client path '{clientPath}'.");
        }

        string suffix = configuration.Aaas.Local ?? false ? ".local" : configuration.Aaas.Dev ?? false ? ".dev" : string.Empty;
        return $"api.{target}{suffix}";
    }

    private string buildHubUrl(ApiClientDto apiClient, SignalRClientRegistration registration)
    {
        string hostBase = $"https://{apiClient.HostUri}";
        string clientSegment = resolveClientSegment(registration);
        string resourceSegment = resolveResourceSegment(registration);
        string combinedPath = string.IsNullOrWhiteSpace(clientSegment)
            ? resourceSegment
            : $"{clientSegment}/{resourceSegment}";
        return UrlHelper.UrlCombine(hostBase, combinedPath);
    }

    private IReadOnlyDictionary<string, string> buildHeaders()
    {
        using HttpRequestMessage request = new();
        RequestIdentityContextHeaders.Apply(request.Headers, identityContext);
        return request.Headers.ToDictionary(static header => header.Key, static header => header.Value.LastOrDefault() ?? string.Empty);
    }

    private async Task<string> getAccessTokenAsync(ApiClientDto apiClient)
    {
        string? token = await apiClientService.GetAccessTokenAsync(apiClient).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(token)
            ? throw new InvalidOperationException($"Unable to acquire access token for client '{apiClient.Id}'.")
            : token;
    }

    private string resolveClientSegment(SignalRClientRegistration registration)
    {
        string? candidate = registration.ClientId ?? identityContext.ClientId;
        return string.IsNullOrWhiteSpace(candidate)
            ? throw new InvalidOperationException("SignalR client registration requires a client id to build the hub path.")
            : normalizeSegment(candidate);
    }

    private string resolveResourceSegment(SignalRClientRegistration registration)
    {
        string? candidate = registration.ResourceId ?? identityContext.ResourceId;
        return string.IsNullOrWhiteSpace(candidate)
            ? throw new InvalidOperationException("SignalR client registration requires a resource id to build the hub path.")
            : normalizeSegment(candidate);
    }

    private static string normalizeSegment(string path)
    {
        string normalized = string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new InvalidOperationException("SignalR client registration requires non-empty path segments.")
            : normalized;
    }
}
