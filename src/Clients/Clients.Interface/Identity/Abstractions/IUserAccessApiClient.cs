using Vitarish.Clients.Interface.Identity.Contracts;

namespace Vitarish.Clients.Interface.Identity.Abstractions;

public interface IUserAccessApiClient
{
    Task<IEnumerable<ClaimDto>?> GetUserClaims(string userId, CancellationToken cancellationToken = default);
    Task<ClaimDto?> GetUserAccessClaim(string userId, string clientId, CancellationToken cancellationToken = default);
    Task<bool> HasUserAccess(string userId, string clientId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserDto>?> GetClientUsers(string clientId, CancellationToken cancellationToken = default);
    Task<UserDto?> GetClientUser(string clientId, string userId, CancellationToken cancellationToken = default);
    Task SetUserAccess(string userId, string clientId, string accessValue, CancellationToken cancellationToken = default);
    Task RemoveUserAccess(string userId, string clientId, CancellationToken cancellationToken = default);
}
