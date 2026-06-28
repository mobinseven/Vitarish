using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class BeginPasskeyLoginHandler(IWebAuthnAuthenticationService webAuthn)
{
    public async Task<AuthenticationResult<ChallengeResult>> Handle(BeginPasskeyLoginCommand command, CancellationToken cancellationToken)
    {
        WebAuthnBeginResult result = await webAuthn.BeginAsync(command.UserNameOrEmail, cancellationToken);
        return new AuthenticationResult<ChallengeResult>.Success(new(result.ChallengeId, Domain.Challenges.AuthenticationChallengePurpose.PasskeyLogin, result.ExpiresAt, result.RemainingAttempts));
    }
}
