using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Domain.Methods;

namespace Vitarish.Authentication.Application.Abstractions;

/// <summary>
/// Contract for updating user account details during authenticated operations.
/// Implemented in Authentication.Infrastructure using Users.Application services.
/// </summary>
public interface IUserAccountUpdater
{
    Task<bool> SetVerifiedPhoneNumberAsync(string userId, string phoneNumber, CancellationToken cancellationToken = default);
    Task<bool> SetVerifiedEmailAsync(string userId, string email, CancellationToken cancellationToken = default);
    Task ConfirmContactAsync(string userId, string contact, CancellationToken cancellationToken = default);
    Task<PasswordChangeOutcome> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<PasswordChangeOutcome> AddFirstPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default);
    Task<PasswordChangeOutcome> ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default);
    Task<bool> SetTwoFactorEnabledAsync(string userId, bool enabled, bool expectedCurrentValue, AuthenticationMethod? expectedChannel, CancellationToken cancellationToken = default);
    Task<bool> PasswordMatchesAsync(string userId, string password, CancellationToken cancellationToken = default);
    Task<bool> UserHasPasswordAsync(string userId, CancellationToken cancellationToken = default);
}

public enum PasswordChangeStatus
{
    Succeeded,
    InvalidCurrentPassword,
    PolicyRejected,
    AlreadyHasPassword,
    UserNotFound,
    Failed
}

public sealed record PasswordChangeOutcome(
    PasswordChangeStatus Status,
    IReadOnlyList<PasswordPolicyViolation> PolicyViolations)
{
    public bool Succeeded => Status is PasswordChangeStatus.Succeeded;

    public static PasswordChangeOutcome Success() => new(PasswordChangeStatus.Succeeded, []);

    public static PasswordChangeOutcome Failure(PasswordChangeStatus status) => new(status, []);

    public static PasswordChangeOutcome PolicyFailure(IReadOnlyList<PasswordPolicyViolation> violations) =>
        new(PasswordChangeStatus.PolicyRejected, violations);
}
