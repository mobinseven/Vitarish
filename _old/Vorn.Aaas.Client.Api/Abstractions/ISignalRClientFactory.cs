
using Microsoft.AspNetCore.SignalR.Client;
using Vorn.Aaas.Client.Api.Options;

namespace Vorn.Aaas.Client.Api.Abstractions;
/// <summary>
/// Creates configured <see cref="HubConnection"/> instances for AAAS protected hubs.
/// </summary>
public interface ISignalRClientFactory
{
    /// <summary>
    /// Creates a <see cref="HubConnection"/> for the specified registration name.
    /// </summary>
    Task<HubConnection> CreateConnection(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a <see cref="HubConnection"/> for the specified registration name using the provided options.
    /// </summary>
    Task<HubConnection> CreateConnection(string name, SignalRConnectionOptions? options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a <see cref="HubConnection"/> for the registration associated with <typeparamref name="TClient"/>.
    /// </summary>
    Task<HubConnection> CreateConnection<TClient>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a <see cref="HubConnection"/> for the registration associated with <typeparamref name="TClient"/> using the provided options.
    /// </summary>
    Task<HubConnection> CreateConnection<TClient>(SignalRConnectionOptions? options, CancellationToken cancellationToken = default);
}
