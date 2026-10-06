using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class CompletePhoneLoginHandler(IAuthenticationChallengeStore store, IAuthenticationCodeVerifier verifier, IAuthenticationSignInService signInService, IAuthenticationRedirectResolver redirects, IAuthenticationAttemptTracker attemptTracker, IAuthenticationSecurityPolicyProvider securityPolicyProvider, IAuthenticationAttemptKeyFactory attemptKeyFactory, AuthenticationAttemptAuditor attemptAuditor)
{
    public async Task<AuthenticationResult<LoginResult>> Handle(CompletePhoneLoginCommand command, CancellationToken cancellationToken)
    {
        string? knownUserId = null;
        AuthenticationResult<LoginResult> result;
        try
        {
            SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
            if (!securityPolicy.Allows(AuthenticationMethod.Phone, PreAuthenticationAction.PhoneLogin))
            {
                result = new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Phone authentication is not enabled."));
            }
            else
            {
                knownUserId = await GetChallengeUserIdAsync(command.ChallengeId, cancellationToken);
                result = await ChallengeCompletionHelper.CompleteChallengeAsync(command.ChallengeId, command.Code, command.ReturnUrl, AuthenticationChallengePurpose.PhoneLogin, store, verifier, signInService, redirects, cancellationToken, attemptTracker, securityPolicy, attemptKeyFactory);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await attemptAuditor.RecordAsync(AuthenticationMethod.Phone, knownUserId, succeeded: false, cancellationToken: CancellationToken.None);
            throw;
        }

        string? subjectId = result is AuthenticationResult<LoginResult>.Success success ? success.Value.UserId : knownUserId;
        try
        {
            await attemptAuditor.RecordAsync(
                AuthenticationMethod.Phone,
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

    private async Task<string?> GetChallengeUserIdAsync(string challengeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(challengeId)) return null;
        AuthenticationChallenge? challenge = await store.GetAsync(AuthenticationChallengeId.Create(challengeId), cancellationToken);
        return challenge?.UserId;
    }
}
