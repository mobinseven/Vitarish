using Vitarish.AuthenticationClients.Application.Commands;
using Vitarish.AuthenticationClients.Domain.Entities;

namespace Vitarish.AuthenticationClients.Application.Abstractions;
/// <summary>
/// Contract for working with AuthenticationClients inside the application (no HTTP required).
/// </summary>
public interface IAuthenticationClientsService
{
    Task<AuthenticationClient?> GetAuthenticationClientAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<AuthenticationClient>> GetAuthenticationClientsAsync(CancellationToken ct = default);
    /// <summary>
    /// Creates a client and returns its assigned id (mirrors the Created location of the HTTP API).
    /// </summary>
    Task<AuthenticationClient?> CreateAuthenticationClientAsync(CreateAuthenticationClient request,
                                                                 CancellationToken ct = default);

    /// <summary>
    /// Updates an existing authentication client.
    /// </summary>
    Task UpdateAuthenticationClientAsync(UpdateAuthenticationClient request,
                                         CancellationToken ct = default);

    /// <summary>
    /// Deletes an authentication client.
    /// </summary>
    Task DeleteAuthenticationClientAsync(DeleteAuthenticationClient request,
                                         CancellationToken ct = default);
}
