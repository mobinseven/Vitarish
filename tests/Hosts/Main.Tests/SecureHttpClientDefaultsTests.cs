using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Vitarish.Hosting;

namespace Vitarish.Main.Tests;

public sealed class SecureHttpClientDefaultsTests
{
    [Fact]
    public void Factory_policy_leaves_ordinary_client_default_timeout_unchanged()
    {
        ServiceCollection services = new();
        _ = services.AddHttpClient("ordinary");
        _ = services.AddSecureHttpClientDefaults();
        using ServiceProvider provider = services.BuildServiceProvider();

        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("ordinary");

        client.Timeout.Should().Be(TimeSpan.FromSeconds(100));
    }

    [Fact]
    public void Factory_policy_configures_tls_and_preserves_explicit_timeout()
    {
        ServiceCollection services = new();
        _ = services.AddHttpClient("configured", client => client.Timeout = TimeSpan.FromSeconds(17));
        _ = services.AddSecureHttpClientDefaults();
        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClientFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>()
            .Get("configured");

        using HttpClient client = new();
        foreach (Action<HttpClient> configureClient in options.HttpClientActions)
        {
            configureClient(client);
        }

        TestHandlerBuilder builder = new() { PrimaryHandler = new HttpClientHandler() };
        foreach (Action<HttpMessageHandlerBuilder> configureHandler in options.HttpMessageHandlerBuilderActions)
        {
            configureHandler(builder);
        }

        client.Timeout.Should().Be(TimeSpan.FromSeconds(17));
        HttpClientHandler handler = builder.PrimaryHandler.Should().BeOfType<HttpClientHandler>().Subject;
        handler.SslProtocols.Should().Be(SslProtocols.Tls12);
        handler.CheckCertificateRevocationList.Should().BeTrue();
        handler.ServerCertificateCustomValidationCallback.Should().BeNull();
    }

    [Fact]
    public void Factory_policy_configures_sockets_handler_without_changing_client_timeout()
    {
        ServiceCollection services = new();
        _ = services.AddHttpClient("configured", client => client.Timeout = TimeSpan.FromSeconds(23))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler());
        _ = services.AddSecureHttpClientDefaults();
        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClientFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>()
            .Get("configured");

        using HttpClient client = new();
        foreach (Action<HttpClient> configureClient in options.HttpClientActions)
        {
            configureClient(client);
        }

        TestHandlerBuilder builder = new() { PrimaryHandler = new SocketsHttpHandler() };
        foreach (Action<HttpMessageHandlerBuilder> configureHandler in options.HttpMessageHandlerBuilderActions)
        {
            configureHandler(builder);
        }

        client.Timeout.Should().Be(TimeSpan.FromSeconds(23));
        SocketsHttpHandler handler = builder.PrimaryHandler.Should().BeOfType<SocketsHttpHandler>().Subject;
        handler.SslOptions.EnabledSslProtocols.Should().Be(SslProtocols.Tls12);
        handler.SslOptions.CertificateRevocationCheckMode.Should().Be(X509RevocationMode.Online);
    }

    private sealed class TestHandlerBuilder : HttpMessageHandlerBuilder
    {
        public override string Name { get; set; } = Options.DefaultName;
        public override HttpMessageHandler PrimaryHandler { get; set; } = new HttpClientHandler();
        public override IList<DelegatingHandler> AdditionalHandlers { get; } = [];

        public override HttpMessageHandler Build() => PrimaryHandler;
    }
}
