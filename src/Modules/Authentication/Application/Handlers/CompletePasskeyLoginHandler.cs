using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class CompletePasskeyLoginHandler(IWebAuthnAuthenticationService webAuthn, IAuthenticationSignInService signInService, IAuthenticationRedirectResolver redirects)
{
    public async Task<AuthenticationResult<LoginResult>> Handle(CompletePasskeyLoginCommand command, CancellationToken cancellationToken)
    {
        WebAuthnCompleteResult result = await webAuthn.CompleteAsync(command.ChallengeId, command.CredentialJson, cancellationToken);
        if (!result.Succeeded)
            return new AuthenticationResult<LoginResult>.Failure(result.Failure ?? new AuthenticationFailure(AuthenticationFailureReason.InvalidCredentials, "Passkey authentication failed."));

        await signInService.SignInAsync(result.UserId!, false, cancellationToken);
        string resolvedReturnUrl = await redirects.ResolveAsync(command.ReturnUrl, false, result.UserId, cancellationToken);
        return new AuthenticationResult<LoginResult>.Success(new(result.UserId!, resolvedReturnUrl, false));
    }
}
