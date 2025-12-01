using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Users.Domain.Entities;

namespace Vitarish.Identity.Infrastructure.Users.Services;

/// <summary>
/// Contract for working with Users inside the application (no HTTP required).
/// </summary>
public interface IUsersService
{
    Task<IEnumerable<User>> GetUsersAsync(CancellationToken ct = default);
    Task<User?> GetUserAsync(string id, CancellationToken ct = default);
    Task<User?> GetUserByNameAsync(string userName, CancellationToken ct = default);
    Task<Result<User>> CreateUserWithPhoneAsync(string phoneNumber, string? firstName, string? lastName, CancellationToken ct = default);
    Task<Result<User>> CreateUserWithEmailAsync(string email, string? firstName, string? lastName, CancellationToken ct = default);
    Task<Result> UpdateUserAsync(string id, string? firstName, string? lastName, CancellationToken ct = default);
    Task<Result> DeleteUserAsync(string id, CancellationToken ct = default);
    Task<Result> SetUserPhoneNumberAsync(string userId, string phoneNumber, CancellationToken ct = default);
    Task<Result> SetUserEmailAsync(string userId, string email, CancellationToken ct = default);
    Task<Dictionary<string, string>> GetUserProfileAsync(string userId, CancellationToken ct = default);
    Task<Result> UpdateUserProfileAsync(string userId, Dictionary<string, string> preferences, CancellationToken ct = default);
}
