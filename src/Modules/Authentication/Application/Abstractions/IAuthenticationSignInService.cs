namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationSignInService
{
    Task<PasswordSignInResult> PasswordSignInAsync(string login, string password, bool rememberMe, CancellationToken cancellationToken);
    Task<PasswordSignInResult> CheckPasswordSignInAsync(string login, string password, CancellationToken cancellationToken);
    Task SignInAsync(string userId, bool rememberMe, CancellationToken cancellationToken);
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
    ProviderFailure
}
