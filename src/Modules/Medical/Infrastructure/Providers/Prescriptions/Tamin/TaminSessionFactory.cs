using Microsoft.Extensions.Options;
using Vorn.Tamin;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminSessionFactory(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<TaminOptions> optionsMonitor) : ITaminSessionFactory
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IOptionsMonitor<TaminOptions> _optionsMonitor = optionsMonitor;

    public TaminSession CreateTokenSession(string oauthToken, bool needToken = true)
    {
        TaminOptions options = _optionsMonitor.CurrentValue;
        return new TaminSession(
            _httpClientFactory.CreateClient("TaminClient"),
            oauthToken,
            ResolveBaseUri(options),
            needToken,
            options.ApiKey,
            options.EffectiveEndpoint.ToSdkEndpoint());
    }

    public TaminSession CreateReferenceDataSession()
    {
        TaminOptions options = _optionsMonitor.CurrentValue;
        return new TaminSession(
            _httpClientFactory.CreateClient("TaminClient"),
            oauthToken: string.Empty,
            baseUri: ResolveBaseUri(options),
            needToken: options.ReferenceDataRequiresToken,
            clientId: options.ApiKey,
            endpoint: options.EffectiveEndpoint.ToSdkEndpoint());
    }

    public Task<TaminSession> CreatePasswordSessionAsync(string username, string password, CancellationToken cancellationToken)
    {
        TaminOptions options = _optionsMonitor.CurrentValue;
        return TaminSession.CreateAsync(
            _httpClientFactory.CreateClient("TaminClient"),
            oauthToken: string.Empty,
            baseUri: ResolveBaseUri(options),
            username: username,
            password: password,
            otp: string.Empty,
            providerIdentifier: string.Empty,
            clientId: options.ApiKey,
            needToken: true,
            cancellationToken: cancellationToken,
            endpoint: options.EffectiveEndpoint.ToSdkEndpoint());
    }

    public AuthClient CreateAuthClient()
    {
        TaminOptions options = _optionsMonitor.CurrentValue;
        return new AuthClient(
            _httpClientFactory.CreateClient("TaminClient"),
            options.EffectiveEndpoint.ToSdkEndpoint(),
            new TaminEnvironmentRoutes());
    }

    private static Uri ResolveBaseUri(TaminOptions options)
    {
        string value = options.EffectiveBaseUrl;
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
        {
            throw new InvalidOperationException("Tamin EffectiveBaseUrl must be an absolute URI.");
        }

        return uri;
    }
}
