using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class BeginPasskeyLoginHandler(IWebAuthnAuthenticationService webAuthn, IAuthenticationAttemptTracker attemptTracker, IAuthenticationSecurityPolicyProvider securityPolicyProvider, IAuthenticationAttemptKeyFactory attemptKeyFactory)
{
    public async Task<AuthenticationResult<ChallengeResult>> Handle(BeginPasskeyLoginCommand command, CancellationToken cancellationToken)
    {
        SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
        if (!securityPolicy.Allows(AuthenticationMethod.Passkey, PreAuthenticationAction.PasskeyLogin))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Passkey authentication is not enabled."));

        if (!string.IsNullOrWhiteSpace(command.UserNameOrEmail))
        {
            foreach (string identifierKey in attemptKeyFactory.IdentifierCandidates(AuthenticationMethod.Passkey, command.UserNameOrEmail))
            {
                if (!await attemptTracker.IsDeniedAsync(identifierKey, cancellationToken))
                    continue;
                return new AuthenticationResult<ChallengeResult>.Failure(new(
                    securityPolicy.FailedAttemptResponse is FailedAttemptResponse.Lockout ? AuthenticationFailureReason.LockedOut : AuthenticationFailureReason.TooManyAttempts,
                    securityPolicy.FailedAttemptResponse is FailedAttemptResponse.Lockout ? "Authentication is temporarily locked out." : "Too many authentication attempts."));
            }
        }

        WebAuthnBeginResult result = await webAuthn.BeginAsync(command.UserNameOrEmail, cancellationToken);
        return new AuthenticationResult<ChallengeResult>.Success(new(
            result.ChallengeId,
            Domain.Challenges.AuthenticationChallengePurpose.PasskeyLogin,
            result.ExpiresAt,
            securityPolicy.FailedAttemptThreshold,
            result.OptionsJson));
    }
}
