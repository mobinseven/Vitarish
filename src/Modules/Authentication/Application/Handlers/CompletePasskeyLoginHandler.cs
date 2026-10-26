using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class CompletePasskeyLoginHandler(
    IWebAuthnAuthenticationService webAuthn,
    IAuthenticationSignInService signInService,
    IAuthenticationRedirectResolver redirects,
    IAuthenticationSecurityPolicyProvider securityPolicyProvider,
    IAuthenticationAttemptTracker attemptTracker,
    IAuthenticationAttemptKeyFactory attemptKeyFactory,
    AuthenticationAttemptAuditor attemptAuditor,
    IAuthenticationSessionRegistry sessionRegistry)
{
    public async Task<AuthenticationResult<LoginResult>> Handle(CompletePasskeyLoginCommand command, CancellationToken cancellationToken)
    {
        string? knownUserId = null;
        AuthenticationResult<LoginResult> result;
        try
        {
            knownUserId = await webAuthn.GetSubjectIdAsync(command.ChallengeId, command.CredentialJson, cancellationToken);
            result = await HandleCoreAsync(command, knownUserId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception failure)
        {
            await attemptAuditor.RecordAsync(AuthenticationMethod.Passkey, knownUserId, succeeded: false, cancellationToken: CancellationToken.None);
            try
            {
                await sessionRegistry.RecordFailedAttemptAsync(knownUserId, CancellationToken.None);
            }
            catch (Exception historyFailure)
            {
                // Keep the original failure visible alongside the access-history write that also failed.
                throw new AggregateException(failure, historyFailure);
            }

            throw;
        }

        string? subjectId = result is AuthenticationResult<LoginResult>.Success success ? success.Value.UserId : knownUserId;
        try
        {
            await attemptAuditor.RecordAsync(
                AuthenticationMethod.Passkey,
                subjectId,
                result is AuthenticationResult<LoginResult>.Success,
                cancellationToken,
                AuthenticationSecurityAuditEvents.FailureReason(result));
        }
        catch (OperationCanceledException)
        {
            if (result is AuthenticationResult<LoginResult>.Success)
                await signInService.SignOutAsync(CancellationToken.None);
            throw;
        }
        return result;
    }

    private async Task<AuthenticationResult<LoginResult>> HandleCoreAsync(CompletePasskeyLoginCommand command, string? knownUserId, CancellationToken cancellationToken)
    {
        SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
        if (!securityPolicy.Allows(AuthenticationMethod.Passkey, PreAuthenticationAction.PasskeyLogin))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Passkey authentication is not enabled."));

        List<string> attemptKeys = [.. attemptKeyFactory.ChallengeCandidates(AuthenticationMethod.Passkey, command.ChallengeId)];
        string? attemptKey = await webAuthn.GetAttemptKeyAsync(command.ChallengeId, command.CredentialJson, cancellationToken);
        if (!string.IsNullOrWhiteSpace(attemptKey) && !attemptKeys.Contains(attemptKey, StringComparer.Ordinal))
            attemptKeys.Add(attemptKey);
        foreach (string key in attemptKeys)
            if (await attemptTracker.IsDeniedAsync(key, cancellationToken))
                return new AuthenticationResult<LoginResult>.Failure(CreateAttemptFailure(securityPolicy));

        WebAuthnCompleteResult result = await webAuthn.CompleteAsync(command.ChallengeId, command.CredentialJson, cancellationToken);
        if (!result.Succeeded)
        {
            int failures = 0;
            foreach (string key in attemptKeys)
            {
                failures = Math.Max(failures, await attemptTracker.RegisterFailureAsync(
                    key,
                    securityPolicy.FailedAttemptThreshold,
                    securityPolicy.FailedAttemptDenialDuration,
                    cancellationToken));
            }
            await sessionRegistry.RecordFailedAttemptAsync(result.UserId ?? knownUserId, cancellationToken);
            if (failures >= securityPolicy.FailedAttemptThreshold)
                return new AuthenticationResult<LoginResult>.Failure(CreateAttemptFailure(securityPolicy));

            return new AuthenticationResult<LoginResult>.Failure(result.Failure ?? new AuthenticationFailure(AuthenticationFailureReason.InvalidCredentials, "Passkey authentication failed."));
        }

        foreach (string key in attemptKeys)
            await attemptTracker.ResetAsync(key, cancellationToken);
        if (!string.IsNullOrWhiteSpace(result.UserId))
            foreach (string subjectKey in attemptKeyFactory.SubjectCandidates(AuthenticationMethod.Passkey, result.UserId))
                await attemptTracker.ResetAsync(subjectKey, cancellationToken);

        AuthenticationSessionSignInResult session = await signInService.SignInAsync(result.UserId!, cancellationToken);
        if (session is not null && !session.Succeeded)
            return new AuthenticationResult<LoginResult>.Failure(session.Failure!);
        string resolvedReturnUrl = await redirects.ResolveAsync(command.ReturnUrl, false, result.UserId, cancellationToken);
        return new AuthenticationResult<LoginResult>.Success(new(result.UserId!, resolvedReturnUrl, false));
    }

    private static AuthenticationFailure CreateAttemptFailure(SecurityAccessPolicy securityPolicy) =>
        securityPolicy.FailedAttemptResponse is FailedAttemptResponse.Lockout
            ? new(AuthenticationFailureReason.LockedOut, "Authentication is temporarily locked out.")
            : new(AuthenticationFailureReason.TooManyAttempts, "Too many authentication attempts.");
}
