using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class CompleteEmailLoginHandler(IAuthenticationChallengeStore store, IAuthenticationCodeVerifier verifier, IAuthenticationSignInService signInService, IAuthenticationRedirectResolver redirects, IAuthenticationAttemptTracker attemptTracker, IAuthenticationSecurityPolicyProvider securityPolicyProvider, IAuthenticationAttemptKeyFactory attemptKeyFactory, AuthenticationSourceAttemptGuard sourceAttemptGuard, AuthenticationAttemptAuditor attemptAuditor, IInitialPlatformAdministratorGrant initialAdministratorGrant, IUserAccountUpdater contactConfirmer, IAuthenticationSessionRegistry sessionRegistry)
{
    public async Task<AuthenticationResult<LoginResult>> Handle(CompleteEmailLoginCommand command, CancellationToken cancellationToken)
    {
        string? knownUserId = null;
        AuthenticationResult<LoginResult> result;
        try
        {
            SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
            if (!securityPolicy.Allows(AuthenticationMethod.Email, PreAuthenticationAction.EmailLogin))
            {
                result = new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Email authentication is not enabled."));
            }
            else
            {
                result = await ChallengeCompletionHelper.CompleteChallengeAsync(command.ChallengeId, command.Code, command.ReturnUrl, AuthenticationChallengePurpose.EmailLogin, store, verifier, signInService, redirects, cancellationToken, attemptTracker, securityPolicy, attemptKeyFactory, sessionRegistry, sourceAttemptGuard, initialAdministratorGrant, contactConfirmer, userId => knownUserId = userId);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await attemptAuditor.RecordAsync(AuthenticationMethod.Email, knownUserId, succeeded: false, cancellationToken: CancellationToken.None);
            throw;
        }

        string? subjectId = result is AuthenticationResult<LoginResult>.Success success ? success.Value.UserId : knownUserId;
        try
        {
            await attemptAuditor.RecordAsync(
                AuthenticationMethod.Email,
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
}
