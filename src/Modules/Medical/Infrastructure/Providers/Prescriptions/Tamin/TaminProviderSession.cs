using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Net.Http;
using System.Security.Authentication;
using Tamin.Integration.Http;
using ProdAccountClient = Tamin.Client.Account.Prod.ProdAccountClient;
using ProdApiClient = Tamin.Client.Api.Prod.ProdApiClient;
using ProdSoaClient = Tamin.Client.Soa.Prod.ProdSoaClient;
using PilotAccountClient = Tamin.Client.Account.Pilot.PilotAccountClient;
using PilotApiClient = Tamin.Client.Api.Pilot.PilotApiClient;
using PilotSoaClient = Tamin.Client.Soa.Pilot.PilotSoaClient;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminProviderSession(
    ProductionTaminClients? production,
    PilotTaminClients? pilot,
    IReadOnlyList<HttpClient> ownedHttpClients) : IDisposable
{
    public ProductionTaminClients? Production { get; } = production;
    public PilotTaminClients? Pilot { get; } = pilot;
    public bool IsPilot => Pilot is not null;

    public void Dispose()
    {
        foreach (HttpClient httpClient in ownedHttpClients)
        {
            httpClient.Dispose();
        }
    }
}

internal sealed class TaminProviderSessionFactory(
    ILoggerFactory loggerFactory,
    IOptionsMonitor<TaminOptions> optionsMonitor)
{
    public TaminProviderSession CreateAnonymous() =>
        Create(new EmptyAccessTokenProvider(optionsMonitor.CurrentValue.AllowedHosts));

    public TaminProviderSession Create(IAccessTokenProvider tokenProvider)
    {
        TaminOptions options = optionsMonitor.CurrentValue;
        TaminClientBases bases = new(
            RequireBaseUri(options.EffectiveAccountBaseUrl, nameof(options.AccountBaseUrl)),
            RequireBaseUri(options.EffectiveSoaBaseUrl, nameof(options.SoaBaseUrl)),
            RequireBaseUri(options.EffectiveApiBaseUrl, nameof(options.ApiBaseUrl)));
        BaseBearerTokenAuthenticationProvider authentication = new(tokenProvider);
        List<HttpClient> clients = [];

        HttpClientRequestAdapter accountAdapter = CreateAdapter(
            new AnonymousAuthenticationProvider(), bases.Account, clients);
        HttpClientRequestAdapter soaAdapter = CreateAdapter(authentication, bases.Soa, clients);
        HttpClientRequestAdapter apiAdapter = CreateAdapter(authentication, bases.Api, clients);

        if (options.EffectiveEndpoint == TaminEndpointMode.Sandbox)
        {
            return new TaminProviderSession(
                null,
                new PilotTaminClients(
                    new PilotAccountClient(accountAdapter),
                    new PilotSoaClient(soaAdapter),
                    new PilotApiClient(apiAdapter)),
                clients);
        }

        return new TaminProviderSession(
            new ProductionTaminClients(
                new ProdAccountClient(accountAdapter),
                new ProdSoaClient(soaAdapter),
                new ProdApiClient(apiAdapter)),
            null,
            clients);
    }

    private HttpClientRequestAdapter CreateAdapter(
        IAuthenticationProvider authentication,
        Uri baseUri,
        ICollection<HttpClient> ownedClients)
    {
        TaminTransientFaultHandler retry = new()
        {
            InnerHandler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls12,
                CheckCertificateRevocationList = true
            }
        };
        TaminRedactedResponseHandler capture = new(
            loggerFactory.CreateLogger<TaminRedactedResponseHandler>())
        {
            InnerHandler = retry
        };
        HttpClient httpClient = new(capture, disposeHandler: true)
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(100)
        };
        ownedClients.Add(httpClient);
        return new HttpClientRequestAdapter(authentication, httpClient: httpClient)
        {
            BaseUrl = baseUri.ToString().TrimEnd('/')
        };
    }

    private static Uri RequireBaseUri(string value, string name) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri
            : throw new InvalidOperationException($"Tamin {name} must be an absolute HTTPS URI.");
}

internal sealed class TaminRedactedResponseHandler(
    ILogger<TaminRedactedResponseHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        string rawBody = response.Content is null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        int statusCode = (int)response.StatusCode;
        string operationId = request.RequestUri?.AbsolutePath ?? "unknown";
        logger.LogWarning(
            "Tamin API operation {OperationId} failed with HTTP {StatusCode}",
            operationId,
            statusCode);
        response.Dispose();
        throw new TaminApiException(statusCode, operationId, rawBody);
    }
}

internal sealed class EmptyAccessTokenProvider(IEnumerable<string> allowedHosts) : IAccessTokenProvider
{
    public AllowedHostsValidator AllowedHostsValidator { get; } = new(allowedHosts);

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
}
