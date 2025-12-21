namespace Vitarish.Clients.Interface.Identity.Abstractions;

public interface IAccessService
{
    Task InitializeAsync(string? clientId = null);
    void RenameRole(string roleName, string roleNewName);
    AccessOld Access { get; }
    IEnumerable<string> Roles { get; }
    IReadOnlyList<UserDto> Users { get; }
    UserDto? User { get; }

    /* ---------- Queries ---------- */

    Task<IEnumerable<ClaimDto>> GetUserClaimsAsync(string userId, CancellationToken ct = default);

    Task<ClaimDto?> GetUserAccessClaimAsync(string userId, string clientId, CancellationToken ct = default);

    Task<IEnumerable<ClaimDto>> GetUserAccessClaimsAsync(string userId, string claimPrefix, CancellationToken ct = default);

    Task<bool> HasUserAccessAsync(string userId, string clientId, CancellationToken ct = default);

    Task<IEnumerable<UserDto>> GetClientUsersAsync(string clientId, CancellationToken ct = default);

    Task<UserDto?> GetClientUserAsync(string clientId, string userId, CancellationToken ct = default);

    /* ---------- Commands ---------- */

    Task SetUserAccessAsync(string userId, string clientId, string accessValue, CancellationToken ct = default);

    Task RemoveUserAccessAsync(string userId, string clientId, CancellationToken ct = default);
}

