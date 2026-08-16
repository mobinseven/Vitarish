
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Abstractions;
/// <summary>
/// Contract for working with Users inside the application (no HTTP required).
/// </summary>
public interface IUsersService : IUserStore<User>, IUserSecurityStampStore<User>, IUserRoleStore<User>
{
    Task<IEnumerable<User>> GetUsersAsync(CancellationToken ct = default);
    Task<User?> GetUserAsync(string id, CancellationToken ct = default);
    Task<User?> GetUserByRawUserNameAsync(string rawUserName, CancellationToken ct = default);
    Task<User?> CreateUserWithPhoneAsync(CreateUserWithPhoneCommand command, CancellationToken ct = default);
    Task<User?> CreateUserWithEmailAsync(CreateUserWithEmailCommand command, CancellationToken ct = default);
    Task<IdentityResult> UpdateUserAsync(UpdateUserCommand command, CancellationToken ct = default);
    Task<IdentityResult> DeleteUserAsync(DeleteUserCommand command, CancellationToken ct = default);
    Task<IList<Claim>> GetUserClaimsAsync(string userId, CancellationToken ct = default);
    Task<IdentityResult> UpdateUserClaimsAsync(UpdateUserClaimsCommand command, CancellationToken ct = default);
    Task<bool> UserHasPasswordAsync(string userId, CancellationToken ct = default);
    Task<bool> CheckUserPasswordAsync(string userId, string password, CancellationToken ct = default);
    Task<bool> CheckUserEmailInUseAsync(string email, string? excludeUserId = null, CancellationToken ct = default);
    Task<bool> CheckUserPhoneNumberInUseAsync(string phoneNumber, string? excludeUserId = null, CancellationToken ct = default);
    Task<IdentityResult> RemoveUserPasswordAsync(string userId, CancellationToken ct = default);
    Task<IdentityResult> AddUserPasswordAsync(string userId, string password, CancellationToken ct = default);
    Task<IdentityResult> SetUserPhoneNumberAsync(string userId, string phoneNumber, CancellationToken ct = default);
    Task<IdentityResult> SetUserEmailAsync(string userId, string email, CancellationToken ct = default);
    Task<IEnumerable<IdentityRole>> GetUserRolesAsync(string userId, CancellationToken ct = default);
    Task<IEnumerable<User>> GetUsersByIdentificationCodeAsync(string identificationCode, CancellationToken ct = default);
    Task<IEnumerable<User>> SearchUsersAsync(string query, int maxResults, CancellationToken ct = default);
    Task<bool> CheckUserRoleAsync(string userId, string roleName, CancellationToken ct = default);
    Task AddToTenantRoleAsync(User user, string roleName, Guid tenantId, CancellationToken ct = default);
    Task RemoveFromTenantRoleAsync(User user, string roleName, Guid tenantId, CancellationToken ct = default);
}
