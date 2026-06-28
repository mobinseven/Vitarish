using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class CompleteEmailLoginHandler(IAuthenticationChallengeStore store, IAuthenticationCodeVerifier verifier, IAuthenticationSignInService signInService, IAuthenticationRedirectResolver redirects)
{
    public async Task<AuthenticationResult<LoginResult>> Handle(CompleteEmailLoginCommand command, CancellationToken cancellationToken)
    {
        return await ChallengeCompletionHelper.CompleteChallengeAsync(command.ChallengeId, command.Code, command.ReturnUrl, AuthenticationChallengePurpose.EmailLogin, store, verifier, signInService, redirects, cancellationToken);
    }
}
