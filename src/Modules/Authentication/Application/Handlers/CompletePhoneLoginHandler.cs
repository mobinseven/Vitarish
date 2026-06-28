using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class CompletePhoneLoginHandler(IAuthenticationChallengeStore store, IAuthenticationCodeVerifier verifier, IAuthenticationSignInService signInService, IAuthenticationRedirectResolver redirects)
{
    public async Task<AuthenticationResult<LoginResult>> Handle(CompletePhoneLoginCommand command, CancellationToken cancellationToken)
    {
        return await ChallengeCompletionHelper.CompleteChallengeAsync(command.ChallengeId, command.Code, command.ReturnUrl, AuthenticationChallengePurpose.PhoneLogin, store, verifier, signInService, redirects, cancellationToken);
    }
}
