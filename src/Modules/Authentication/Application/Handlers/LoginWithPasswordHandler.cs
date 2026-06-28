using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class LoginWithPasswordHandler(
    IAuthenticationSignInService signInService,
    IAuthenticationRedirectResolver redirects)
{
    public async Task<AuthenticationResult<LoginResult>> Handle(LoginWithPasswordCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.UserNameOrEmail) || !AuthenticationInputValidator.HasText(command.Password))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "Login and password are required."));

        PasswordSignInResult signInResult = await signInService.PasswordSignInAsync(command.UserNameOrEmail, command.Password, command.RememberMe, cancellationToken);

        return signInResult.Status switch
        {
            PasswordSignInStatus.Succeeded =>
                new AuthenticationResult<LoginResult>.Success(new(
                    signInResult.UserId!,
                    redirects.Resolve(command.ReturnUrl, signInResult.RequiresProfileCompletion),
                    signInResult.RequiresProfileCompletion)),
            PasswordSignInStatus.InvalidCredentials or PasswordSignInStatus.UserNotFound =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "Invalid credentials.")),
            PasswordSignInStatus.LockedOut =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.LockedOut, "Account is locked out.")),
            PasswordSignInStatus.NotAllowed =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Sign in is not allowed.")),
            PasswordSignInStatus.RequiresTwoFactor =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.AdditionalStepRequired, "Additional authentication required.")),
            PasswordSignInStatus.ProviderFailure =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, "Authentication provider failure.")),
            _ => new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "Invalid credentials."))
        };
    }
}
