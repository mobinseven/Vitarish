namespace Vitarish.Identity.Core.Services;

using Vitarish.Identity.Core.Domain.Models;

/// <summary>
/// Service interface for managing users.
/// </summary>
public interface IUsersService
{
    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user DTO if found; otherwise, null.</returns>
    Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    /// <param name="userName">The username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user DTO if found; otherwise, null.</returns>
    Task<UserDto?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user DTO if found; otherwise, null.</returns>
    Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with pagination.
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of user DTOs.</returns>
    Task<IEnumerable<UserDto>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user with email.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="password">Optional password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user DTO.</returns>
    Task<UserDto> CreateWithEmailAsync(string email, string? password = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user with phone number.
    /// </summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <param name="password">Optional password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user DTO.</returns>
    Task<UserDto> CreateWithPhoneAsync(string phoneNumber, string? password = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="user">The user DTO with updated values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user DTO.</returns>
    Task<UserDto> UpdateAsync(UserDto user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the claims for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of claim DTOs.</returns>
    Task<IEnumerable<ClaimDto>> GetClaimsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the claims for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="claims">The claims to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateClaimsAsync(string userId, IEnumerable<ClaimDto> claims, CancellationToken cancellationToken = default);
}
