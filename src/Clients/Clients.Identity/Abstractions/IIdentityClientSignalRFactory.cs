using Microsoft.AspNetCore.SignalR.Client;
using Vitarish.Clients.Identity.Options;

namespace Vitarish.Clients.Identity.Abstractions;
/// <summary>
/// Creates configured <see cref="HubConnection"/> instances for AAAS protected hubs.
/// </summary>
public interface IIdentityClientSignalRFactory
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
