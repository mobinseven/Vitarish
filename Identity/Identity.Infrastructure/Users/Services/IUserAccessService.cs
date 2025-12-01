using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Contracts.Users;

namespace Vitarish.Identity.Infrastructure.Users.Services;

/// <summary>
/// In-process abstraction for user access operations.
/// </summary>
public interface IUserAccessService
{
    /* ---------- Queries ---------- */

    Task<bool> HasUserAccessAsync(
        string userId,
        string clientId,
        CancellationToken ct = default);

    Task<IEnumerable<UserDto>> GetClientUsersAsync(
        string clientId,
        CancellationToken ct = default);

    Task<UserDto?> GetClientUserAsync(
        string clientId,
        string userId,
        CancellationToken ct = default);

    /* ---------- Commands ---------- */

    Task<Result> SetUserAccessAsync(
        string userId,
        string clientId,
        string accessValue,
        CancellationToken ct = default);

    Task<Result> RemoveUserAccessAsync(
        string userId,
        string clientId,
        CancellationToken ct = default);
}
