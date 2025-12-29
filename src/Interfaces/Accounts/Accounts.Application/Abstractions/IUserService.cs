using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Accounts.Application.Abstractions;

public interface IUserService
{
    /// <summary>
    /// Get a user by ID, possibly from cache.
    /// </summary>
    Task<User?> GetUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Update a user and invalidate cache.
    /// </summary>
    Task<IdentityResult> UpdateUserAsync(string userId, User user, CancellationToken ct = default);

    /// <summary>
    /// Create a new user using email or phone number.
    /// </summary>
    Task<IdentityResult> CreateUserAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Check whether the user has a password set.
    /// </summary>
    Task<bool> UserHasPasswordAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Check whether the given email is in use, excluding the specified userId.
    /// </summary>
    Task<bool> CheckUserEmailInUseAsync(string email, string? userId, CancellationToken ct = default);

    /// <summary>
    /// Check whether the given phone number is in use, excluding the specified userId.
    /// </summary>
    Task<bool> CheckUserPhoneNumberInUseAsync(string phoneNumber, string? userId, CancellationToken ct = default);

    /// <summary>
    /// Send an email authentication code for the specified user.
    /// </summary>
    Task SendEmailCode(string userId, string email);

    /// <summary>
    /// Send an SMS authentication code for the specified user.
    /// </summary>
    Task SendSmsCode(string userId, string phone);

    /// <summary>
    /// Get the cached profile preferences for the specified user.
    /// </summary>
    Task<Dictionary<string, string>?> GetProfileAsync(string userId);

    /// <summary>
    /// Update profile preferences and update cache.
    /// </summary>
    Task UpdateProfileAsync(string userId, Dictionary<string, string> preferences);

    /// <summary>
    /// Get the current culture preference for the specified user.
    /// </summary>
    Task<string?> GetCultureAsync(string userId);

    /// <summary>
    /// Set the culture preference for the specified user and update cache.
    /// </summary>
    Task<bool> SetCultureAsync(string userId, string culture);
    Task<IEnumerable<IdentityRole>> GetUserRolesAsync(string userId, CancellationToken ct = default);
}
