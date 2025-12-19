using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Contracts;
using Vitarish.Clients.Identity.Options;

namespace Vitarish.Clients.Identity.Internal;

internal class IdentityClientSignalRFactory(
    IIdentityClientService identityClientService,
    IOptions<IdentityClientOptions> identityOptions,
    IOptions<SignalRClientOptions> signalROptions) : IIdentityClientSignalRFactory
{
    private readonly SignalRClientOptions registrations = signalROptions.Value;
    private readonly IdentityClientOptions configuration = identityOptions.Value;

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
        HubConnectionBuilder builder = new();
        _ = builder.WithUrl(new Uri(new Uri(identityOptions.Value.Authority), registration.RelativePath), httpOptions =>
        {
            httpOptions.AccessTokenProvider = async () => (string?)await identityClientService.GetAccessTokenAsync().ConfigureAwait(false);
            registration.ConfigureHttpConnection?.Invoke(httpOptions);
        });

        if (registration.AutomaticReconnect)
        {
            _ = builder.WithAutomaticReconnect();
        }

        if (registration.UseMessagePack)
        {
            _ = builder.AddMessagePackProtocol();
        }

        registration.ConfigureBuilder?.Invoke(builder);
        return builder.Build();
    }
}
