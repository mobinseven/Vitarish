using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Vitarish.Hosting;

/// <summary>
/// Applies the product's common outbound HTTP transport policy to clients created
/// by <see cref="IHttpClientFactory"/>.
/// </summary>
public static class SecureHttpClientDefaultsExtensions
{
    public static IServiceCollection AddSecureHttpClientDefaults(this IServiceCollection services)
    {
        services.ConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(builder => ConfigureHandler(builder.PrimaryHandler));
        });

        return services;
    }

    private static void ConfigureHandler(HttpMessageHandler handler)
    {
        switch (handler)
        {
            case HttpClientHandler httpClientHandler:
                httpClientHandler.SslProtocols = SslProtocols.Tls12;
                httpClientHandler.CheckCertificateRevocationList = true;
                return;

            case SocketsHttpHandler socketsHttpHandler:
                socketsHttpHandler.SslOptions.EnabledSslProtocols = SslProtocols.Tls12;
                socketsHttpHandler.SslOptions.CertificateRevocationCheckMode = X509RevocationMode.Online;
                return;

            default:
                throw new InvalidOperationException(
                    $"The secure HTTP policy cannot configure handler {handler.GetType().FullName}.");
        }
    }
}
