namespace Vitarish.Authentication.Application.Abstractions;

/// <summary>
/// Contract for updating user account details during authenticated operations.
/// Implemented in Authentication.Infrastructure using Users.Application services.
/// </summary>
public interface IUserAccountUpdater
{
    Task<bool> SetPhoneNumberAsync(string userId, string phoneNumber, CancellationToken cancellationToken = default);
    Task<bool> SetEmailAsync(string userId, string email, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<bool> UserHasPasswordAsync(string userId, CancellationToken cancellationToken = default);
}
