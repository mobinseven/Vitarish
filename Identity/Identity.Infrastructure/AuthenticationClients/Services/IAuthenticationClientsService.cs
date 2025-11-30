using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Contracts.AuthenticationClients;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;

namespace Vitarish.Identity.Infrastructure.AuthenticationClients.Services;

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
    Task<Result<AuthenticationClient>> CreateAuthenticationClientAsync(CreateAuthenticationClientRequest request,
                                                                 CancellationToken ct = default);

    /// <summary>
    /// Updates an existing authentication client.
    /// </summary>
    Task<Result> UpdateAuthenticationClientAsync(UpdateAuthenticationClientRequest request,
                                         CancellationToken ct = default);

    /// <summary>
    /// Deletes an authentication client.
    /// </summary>
    Task<Result> DeleteAuthenticationClientAsync(DeleteAuthenticationClientRequest request,
                                         CancellationToken ct = default);
}
