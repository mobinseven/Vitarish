using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class CompletePhoneLoginHandler(IAuthenticationChallengeStore store, IAuthenticationCodeVerifier verifier, IAuthenticationSignInService signInService, IAuthenticationRedirectResolver redirects, IAuthenticationAttemptTracker attemptTracker, IAuthenticationSecurityPolicyProvider securityPolicyProvider, IAuthenticationAttemptKeyFactory attemptKeyFactory)
{
    public async Task<AuthenticationResult<LoginResult>> Handle(CompletePhoneLoginCommand command, CancellationToken cancellationToken)
    {
        SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
        if (!securityPolicy.Allows(AuthenticationMethod.Phone, PreAuthenticationAction.PhoneLogin))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Phone authentication is not enabled."));

        return await ChallengeCompletionHelper.CompleteChallengeAsync(command.ChallengeId, command.Code, command.ReturnUrl, AuthenticationChallengePurpose.PhoneLogin, store, verifier, signInService, redirects, cancellationToken, attemptTracker, securityPolicy, attemptKeyFactory);
    }
}
