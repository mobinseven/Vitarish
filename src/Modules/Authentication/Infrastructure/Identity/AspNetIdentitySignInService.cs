using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AspNetIdentitySignInService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IAuthenticationSecurityPolicyProvider policyProvider,
    IAuthenticationAttemptTracker attemptTracker,
    UnknownPasswordVerifier unknownPasswordVerifier,
    IAuthenticationAttemptKeyFactory attemptKeyFactory,
    IAuthenticationSessionRegistry sessionRegistry,
    IHttpContextAccessor httpContextAccessor,
    IEnumerable<IRoleFreeSessionPolicy>? roleFreeSessions = null) : IAuthenticationSignInService
{
    public Task<PasswordSignInResult> PasswordSignInAsync(string login, string password, CancellationToken cancellationToken) =>
        SignInCoreAsync(login, password, cancellationToken, signIn: true);

    public Task<PasswordSignInResult> CheckPasswordSignInAsync(string login, string password, CancellationToken cancellationToken) =>
        SignInCoreAsync(login, password, cancellationToken, signIn: false);

    public async Task<AuthenticationSessionSignInResult> SignInAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return AuthenticationSessionSignInResult.Failed(new(AuthenticationFailureReason.InvalidInput, "User ID cannot be empty."));
        User? user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return AuthenticationSessionSignInResult.Failed(new(AuthenticationFailureReason.InvalidCredentials, "Cannot sign in an unknown user."));
        if (!user.IsEnabled)
            return AuthenticationSessionSignInResult.Failed(new(AuthenticationFailureReason.AccountDisabled, "The account is disabled."));
        if (!await MayStartSessionAsync(user, cancellationToken))
            return AuthenticationSessionSignInResult.Failed(new(AuthenticationFailureReason.NoAccess, "The account has no access level."));

        IAuthenticationSessionPreparation? preparation = null;
        preparation = await sessionRegistry.PrepareAsync(userId, cancellationToken);
        if (!preparation.Result.Succeeded)
        {
            await preparation.DisposeAsync();
            return AuthenticationSessionSignInResult.Failed(preparation.Result.Failure!);
        }
        AuthenticationSessionEstablishmentResult established = preparation.Result;
        try
        {
            ClaimsPrincipal principal = await signInManager.CreateUserPrincipalAsync(user);
            ClaimsIdentity identity = (ClaimsIdentity)principal.Identity!;
            identity.AddClaim(new Claim(AuthenticationSessionClaimTypes.SessionId, established.SessionId!));
            DateTimeOffset issuedUtc = DateTimeOffset.UtcNow;
            await httpContextAccessor.HttpContext!.SignInAsync(
                IdentityConstants.ApplicationScheme,
                principal,
                new AuthenticationProperties
                {
                    AllowRefresh = true,
                    IssuedUtc = issuedUtc,
                    ExpiresUtc = issuedUtc.AddMinutes(established.InactivityTimeoutMinutes!.Value)
                });
            await preparation.CompleteAsync(cancellationToken);
        }
        catch
        {
            if (preparation is not null)
                await preparation.AbortAsync(CancellationToken.None);
            httpContextAccessor.HttpContext?.Response.Cookies.Delete(IdentityConstants.ApplicationScheme);
            throw;
        }
        finally
        {
            if (preparation is not null)
                await preparation.DisposeAsync();
        }

        // An administrator may have disabled the account after the check above. The
        // disable command re-checks for sessions after saving; this re-check after the
        // session is committed closes the other ordering, so neither can be missed.
        if (!await IsAccountEnabledAsync(userId, cancellationToken))
        {
            await sessionRegistry.RevokeAsync(established.SessionId!, AuthenticationSessionTerminationReason.SecurityAttributeChanged, CancellationToken.None);
            await httpContextAccessor.HttpContext!.SignOutAsync(IdentityConstants.ApplicationScheme);
            return AuthenticationSessionSignInResult.Failed(new(AuthenticationFailureReason.AccountDisabled, "The account is disabled."));
        }

        // The account may also have lost its last role while the session was
        // being committed. Role removal rejects active sessions; this re-check
        // closes the other ordering, so neither can be missed.
        if (!await MayStartSessionAsync(user, cancellationToken))
        {
            await sessionRegistry.RevokeAsync(established.SessionId!, AuthenticationSessionTerminationReason.SecurityAttributeChanged, CancellationToken.None);
            await httpContextAccessor.HttpContext!.SignOutAsync(IdentityConstants.ApplicationScheme);
            return AuthenticationSessionSignInResult.Failed(new(AuthenticationFailureReason.NoAccess, "The account has no access level."));
        }

        return AuthenticationSessionSignInResult.Success();
    }

    // A session needs a role, except on a tenant that one of the registered policies admits for role-free
    // sessions (the platform hosts that hold no practice data).
    private async Task<bool> MayStartSessionAsync(User user, CancellationToken cancellationToken)
    {
        IList<string> roles = await userManager.GetRolesAsync(user);
        if (UserAccessLevelPolicy.HasAccessLevel(roles))
            return true;
        // Sequential on purpose: a policy may read scoped persistence.
        foreach (IRoleFreeSessionPolicy policy in roleFreeSessions ?? [])
        {
            if (await policy.AllowsRoleFreeSessionAsync(cancellationToken))
                return true;
        }

        return false;
    }

    private Task<bool> IsAccountEnabledAsync(string userId, CancellationToken cancellationToken) =>
        userManager.Users.AsNoTracking()
            .Where(candidate => candidate.Id == userId)
            .Select(candidate => candidate.IsEnabled)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        string? sessionId = httpContextAccessor.HttpContext?.User.FindFirstValue(AuthenticationSessionClaimTypes.SessionId);
        if (!string.IsNullOrWhiteSpace(sessionId))
            await sessionRegistry.RevokeAsync(sessionId, AuthenticationSessionTerminationReason.UserLogout, cancellationToken);
        await signInManager.SignOutAsync();
    }

    private async Task<PasswordSignInResult> SignInCoreAsync(string login, string password, CancellationToken cancellationToken, bool signIn)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return new PasswordSignInResult(PasswordSignInStatus.InvalidCredentials, null, false);

        SecurityAccessPolicy policy = await policyProvider.GetAsync(cancellationToken);
        IReadOnlyList<string> identifierKeys = attemptKeyFactory.IdentifierCandidates(AuthenticationMethod.Password, login);
        if (await IsDeniedAsync(identifierKeys, cancellationToken))
            return DeniedResult(policy.FailedAttemptResponse);

        User? byName = await userManager.FindByNameAsync(login);
        User? byEmail = await userManager.FindByEmailAsync(login);
        User? user = byName ?? byEmail;
        if (user is null)
        {
            IReadOnlyList<string> unknownSubjectKeys = attemptKeyFactory.SubjectCandidates(AuthenticationMethod.Password, login);
            if (await IsDeniedAsync(unknownSubjectKeys, cancellationToken))
                return DeniedResult(policy.FailedAttemptResponse);
            unknownPasswordVerifier.Verify(password);
            int identifierFailures = await RegisterFailuresAsync(identifierKeys, policy, cancellationToken);
            int subjectFailures = await RegisterFailuresAsync(unknownSubjectKeys, policy, cancellationToken);
            int failures = Math.Max(identifierFailures, subjectFailures);
            return failures >= policy.FailedAttemptThreshold
                ? DeniedResult(policy.FailedAttemptResponse)
                : new PasswordSignInResult(PasswordSignInStatus.InvalidCredentials, null, false);
        }

        if (await userManager.IsLockedOutAsync(user))
            return await MaskAccountStateAsync(password, user, identifierKeys, policy, cancellationToken);

        IReadOnlyList<string> subjectKeys = attemptKeyFactory.SubjectCandidates(AuthenticationMethod.Password, user.Id);
        if (await IsDeniedAsync(subjectKeys, cancellationToken))
            return DeniedResult(policy.FailedAttemptResponse);

        SignInResult result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        if (result.Succeeded || result.RequiresTwoFactor)
        {
            if (!user.IsEnabled)
                return new PasswordSignInResult(PasswordSignInStatus.AccountDisabled, user.Id, RequiresProfileCompletion(user));
            // Like a disabled account, refuse right after the password verifies, before
            // any second-factor code is sent.
            if (!await MayStartSessionAsync(user, cancellationToken))
                return new PasswordSignInResult(PasswordSignInStatus.NoAccess, user.Id, RequiresProfileCompletion(user));
            await ResetAsync(identifierKeys, cancellationToken);
            await ResetAsync(subjectKeys, cancellationToken);
            PasswordSignInResult successful = new(
                result.RequiresTwoFactor ? PasswordSignInStatus.RequiresTwoFactor : PasswordSignInStatus.Succeeded,
                user.Id,
                RequiresProfileCompletion(user),
                user.TwoFactorEnabled);
            if (signIn && successful.Status == PasswordSignInStatus.Succeeded)
            {
                AuthenticationSessionSignInResult session = await SignInAsync(user.Id, cancellationToken);
                if (!session.Succeeded)
                    return successful with { Status = StatusForSessionFailure(session.Failure) };
            }
            return successful;
        }

        PasswordSignInStatus status = result.IsLockedOut
            ? PasswordSignInStatus.LockedOut
            : result.IsNotAllowed
                ? PasswordSignInStatus.NotAllowed
                : PasswordSignInStatus.InvalidCredentials;
        if (status is PasswordSignInStatus.LockedOut or PasswordSignInStatus.NotAllowed)
            return await MaskAccountStateAsync(password, user, identifierKeys, policy, cancellationToken);
        if (status == PasswordSignInStatus.InvalidCredentials)
        {
            int identifierFailures = await RegisterFailuresAsync(identifierKeys, policy, cancellationToken);
            int subjectFailures = await RegisterFailuresAsync(subjectKeys, policy, cancellationToken);
            int failures = Math.Max(identifierFailures, subjectFailures);
            if (failures >= policy.FailedAttemptThreshold)
                status = policy.FailedAttemptResponse == FailedAttemptResponse.Lockout
                    ? PasswordSignInStatus.LockedOut
                    : PasswordSignInStatus.TooManyAttempts;
        }

        if (status is not PasswordSignInStatus.Succeeded and not PasswordSignInStatus.RequiresTwoFactor)
            await sessionRegistry.RecordFailedAttemptAsync(user.Id, cancellationToken);

        return new PasswordSignInResult(status, user.Id, RequiresProfileCompletion(user));
    }

    private async Task<PasswordSignInResult> MaskAccountStateAsync(
        string password,
        User user,
        IReadOnlyList<string> identifierKeys,
        SecurityAccessPolicy policy,
        CancellationToken cancellationToken)
    {
        unknownPasswordVerifier.Verify(password);
        IReadOnlyList<string> subjectKeys = attemptKeyFactory.SubjectCandidates(AuthenticationMethod.Password, user.Id);
        int identifierFailures = await RegisterFailuresAsync(identifierKeys, policy, cancellationToken);
        int subjectFailures = await RegisterFailuresAsync(subjectKeys, policy, cancellationToken);
        int failures = Math.Max(identifierFailures, subjectFailures);
        await sessionRegistry.RecordFailedAttemptAsync(user.Id, cancellationToken);
        return failures >= policy.FailedAttemptThreshold
            ? DeniedResult(policy.FailedAttemptResponse)
            : new PasswordSignInResult(PasswordSignInStatus.InvalidCredentials, null, false);
    }

    private static PasswordSignInResult DeniedResult(FailedAttemptResponse response) =>
        new(response == FailedAttemptResponse.Lockout ? PasswordSignInStatus.LockedOut : PasswordSignInStatus.TooManyAttempts, null, false);

    private static PasswordSignInStatus StatusForSessionFailure(AuthenticationFailure? failure) => failure?.Reason switch
    {
        AuthenticationFailureReason.NotAllowed => PasswordSignInStatus.NotAllowed,
        AuthenticationFailureReason.LockedOut => PasswordSignInStatus.LockedOut,
        AuthenticationFailureReason.TooManyAttempts => PasswordSignInStatus.TooManyAttempts,
        AuthenticationFailureReason.AccountDisabled => PasswordSignInStatus.AccountDisabled,
        AuthenticationFailureReason.NoAccess => PasswordSignInStatus.NoAccess,
        _ => PasswordSignInStatus.ProviderFailure
    };

    private static bool RequiresProfileCompletion(User user) =>
        string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName);

    private async Task<int> RegisterFailuresAsync(
        IReadOnlyList<string> keys,
        SecurityAccessPolicy policy,
        CancellationToken cancellationToken)
    {
        int failures = 0;
        foreach (string key in keys)
            failures = Math.Max(failures, await attemptTracker.RegisterFailureAsync(
                key,
                policy.FailedAttemptThreshold,
                policy.FailedAttemptDenialDuration,
                cancellationToken));
        return failures;
    }

    private async Task<bool> IsDeniedAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        foreach (string key in keys)
            if (await attemptTracker.IsDeniedAsync(key, cancellationToken))
                return true;
        return false;
    }

    private async Task ResetAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        foreach (string key in keys)
            await attemptTracker.ResetAsync(key, cancellationToken);
    }
}
