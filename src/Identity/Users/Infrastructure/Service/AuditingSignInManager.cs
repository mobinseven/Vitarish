
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Service;

public sealed class AuditingSignInManager(
    UserManager<User> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<User> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<AuditingSignInManager> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<User> confirmation)
    : SignInManager<User>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    private readonly ILogger<AuditingSignInManager> _logger = logger;

    public override async Task<SignInResult> PasswordSignInAsync(User user, string password, bool isPersistent, bool lockoutOnFailure)
    {
        SignInResult result = await base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
        LogResult(result, user, user.UserName, nameof(PasswordSignInAsync));
        return result;
    }

    public override async Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
    {
        SignInResult result = await base.PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure);
        if (!result.Succeeded)
        {
            User? user = await TryFindByNameAsync(userName);
            LogResult(result, user, userName, nameof(PasswordSignInAsync));
        }

        return result;
    }

    public override async Task<SignInResult> CheckPasswordSignInAsync(User user, string password, bool lockoutOnFailure)
    {
        SignInResult result = await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
        LogResult(result, user, user.UserName, nameof(CheckPasswordSignInAsync));
        return result;
    }

    public override async Task<SignInResult> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent, bool bypassTwoFactor)
    {
        SignInResult result = await base.ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent, bypassTwoFactor);
        if (!result.Succeeded)
        {
            User? user = await UserManager.FindByLoginAsync(loginProvider, providerKey);
            LogResult(result, user, user?.UserName, nameof(ExternalLoginSignInAsync));
        }

        return result;
    }

    public override async Task<SignInResult> TwoFactorAuthenticatorSignInAsync(string code, bool isPersistent, bool rememberClient)
    {
        SignInResult result = await base.TwoFactorAuthenticatorSignInAsync(code, isPersistent, rememberClient);
        if (!result.Succeeded)
        {
            User? user = await GetTwoFactorAuthenticationUserAsync();
            LogResult(result, user, user?.UserName, nameof(TwoFactorAuthenticatorSignInAsync), true);
        }

        return result;
    }

    public override async Task<SignInResult> TwoFactorRecoveryCodeSignInAsync(string recoveryCode)
    {
        SignInResult result = await base.TwoFactorRecoveryCodeSignInAsync(recoveryCode);
        if (!result.Succeeded)
        {
            User? user = await GetTwoFactorAuthenticationUserAsync();
            LogResult(result, user, user?.UserName, nameof(TwoFactorRecoveryCodeSignInAsync), true);
        }

        return result;
    }

    public override async Task<SignInResult> TwoFactorSignInAsync(string provider, string code, bool isPersistent, bool rememberClient)
    {
        SignInResult result = await base.TwoFactorSignInAsync(provider, code, isPersistent, rememberClient);
        if (!result.Succeeded)
        {
            User? user = await GetTwoFactorAuthenticationUserAsync();
            LogResult(result, user, user?.UserName, nameof(TwoFactorSignInAsync), true);
        }

        return result;
    }

    private async Task<User?> TryFindByNameAsync(string userName) => string.IsNullOrWhiteSpace(userName) ? null : await UserManager.FindByNameAsync(userName);

    private void LogResult(SignInResult result, User? user, string? userName, string flow, bool treatMfaFailures = false)
    {
        string? reason = DetermineReason(result, treatMfaFailures);
        if (reason is null)
        {
            return;
        }

        _logger.LogWarning(
            "Authentication failure during {Flow} for {Username} ({UserId}) from {ClientIp}. Reason: {Reason}.",
            flow,
            userName ?? user?.UserName ?? "unknown",
            user?.Id ?? "unknown",
            Context?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            reason);
    }

    private static string? DetermineReason(SignInResult result, bool treatMfaFailures) => result.Succeeded
            ? null
            : result.IsLockedOut
            ? "Lockout"
            : result.IsNotAllowed
            ? "AccessDenied"
            : treatMfaFailures ? "MfaFailure" : result.RequiresTwoFactor ? null : "InvalidCredentials";
}
