namespace Vitarish.Authentication.Application.Abstractions;

using Vitarish.Authentication.Application.Results;

public interface IAuthenticationSignInService
{
    Task<PasswordSignInResult> PasswordSignInAsync(string login, string password, bool rememberMe, CancellationToken cancellationToken);
    Task<PasswordSignInResult> CheckPasswordSignInAsync(string login, string password, CancellationToken cancellationToken);
    Task<AuthenticationSessionSignInResult> SignInAsync(string userId, bool rememberMe, CancellationToken cancellationToken);
    Task SignOutAsync(CancellationToken cancellationToken);
}

public sealed record PasswordSignInResult(
    PasswordSignInStatus Status,
    string? UserId,
    bool RequiresProfileCompletion,
    bool RequiresTwoFactor = false);

public enum PasswordSignInStatus
{
    Succeeded,
    InvalidCredentials,
    LockedOut,
    NotAllowed,
    RequiresTwoFactor,
    UserNotFound,
    ProviderFailure,
    TooManyAttempts
}

public sealed record AuthenticationSessionSignInResult(bool Succeeded, AuthenticationFailure? Failure)
{
    public static AuthenticationSessionSignInResult Success() => new(true, null);
    public static AuthenticationSessionSignInResult Failed(AuthenticationFailure failure) => new(false, failure);
}
