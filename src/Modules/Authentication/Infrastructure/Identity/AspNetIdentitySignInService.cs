using Microsoft.AspNetCore.Identity;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AspNetIdentitySignInService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IAuthenticationSecurityPolicyProvider policyProvider,
    IAuthenticationAttemptTracker attemptTracker,
    UnknownPasswordVerifier unknownPasswordVerifier,
    IAuthenticationAttemptKeyFactory attemptKeyFactory) : IAuthenticationSignInService
{
    public Task<PasswordSignInResult> PasswordSignInAsync(string login, string password, bool rememberMe, CancellationToken cancellationToken) =>
        SignInCoreAsync(login, password, rememberMe, cancellationToken, signIn: true);

    public Task<PasswordSignInResult> CheckPasswordSignInAsync(string login, string password, CancellationToken cancellationToken) =>
        SignInCoreAsync(login, password, false, cancellationToken, signIn: false);

    public async Task SignInAsync(string userId, bool rememberMe, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        User? user = await userManager.FindByIdAsync(userId);
        if (user is null) throw new InvalidOperationException("Cannot sign in an unknown user.");
        await signInManager.SignInAsync(user, rememberMe);
    }

    public Task SignOutAsync(CancellationToken cancellationToken) => signInManager.SignOutAsync();

    private async Task<PasswordSignInResult> SignInCoreAsync(string login, string password, bool rememberMe, CancellationToken cancellationToken, bool signIn)
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

        SignInResult result = signIn
            ? await signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false)
            : await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        if (result.Succeeded || result.RequiresTwoFactor)
        {
            await ResetAsync(identifierKeys, cancellationToken);
            await ResetAsync(subjectKeys, cancellationToken);
            return new PasswordSignInResult(
                result.RequiresTwoFactor ? PasswordSignInStatus.RequiresTwoFactor : PasswordSignInStatus.Succeeded,
                user.Id,
                RequiresProfileCompletion(user),
                user.TwoFactorEnabled);
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
        return failures >= policy.FailedAttemptThreshold
            ? DeniedResult(policy.FailedAttemptResponse)
            : new PasswordSignInResult(PasswordSignInStatus.InvalidCredentials, null, false);
    }

    private static PasswordSignInResult DeniedResult(FailedAttemptResponse response) =>
        new(response == FailedAttemptResponse.Lockout ? PasswordSignInStatus.LockedOut : PasswordSignInStatus.TooManyAttempts, null, false);

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
