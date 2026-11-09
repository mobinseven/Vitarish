using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tamin.Integration;
using Tamin.Integration.Auth;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

/// <summary>
/// Creates the SDK client for one doctor credential. The SDK owns hosts, token binding and the HTTP pipeline;
/// this factory supplies the shared secure transport and the credential's token store.
/// </summary>
internal sealed class TaminClientFactory(
    IHttpMessageHandlerFactory handlerFactory,
    ILoggerFactory loggerFactory,
    IOptionsMonitor<TaminOptions> optionsMonitor)
{
    public const string HttpClientName = "Tamin";

    public ITaminClient Create(ITaminTokenStore tokenStore)
    {
        TaminOptions options = optionsMonitor.CurrentValue;
        TaminClientOptions clientOptions = new(
            Require(options.ApiKey, nameof(options.ApiKey)),
            Require(options.Audience, nameof(options.Audience)),
            handlerFactory.CreateHandler(HttpClientName),
            loggerFactory,
            tokenStore);

        return options.EffectiveEndpoint == TaminEndpointMode.Sandbox
            ? TaminClient.CreatePilot(clientOptions)
            : TaminClient.CreateProduction(clientOptions);
    }

    private static string Require(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"Tamin {name} is required.") : value;
}
