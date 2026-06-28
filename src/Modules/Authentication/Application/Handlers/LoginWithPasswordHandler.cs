using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Queries;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class LoginWithPasswordHandler(IAuthenticationSignInService signInService, IAuthenticationRedirectResolver redirects)
{
    public async Task<AuthenticationResult<LoginResult>> Handle(LoginWithPasswordCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.UserNameOrEmail) || !AuthenticationInputValidator.HasText(command.Password)) return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "Login and password are required."));
        bool signedIn = await signInService.PasswordSignInAsync(command.UserNameOrEmail, command.Password, command.RememberMe, cancellationToken);
        return signedIn ? new AuthenticationResult<LoginResult>.Success(new(command.UserNameOrEmail, redirects.Resolve(command.ReturnUrl, false), false)) : new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "Invalid credentials."));
    }
}

public sealed class BeginPhoneLoginHandler(IAuthenticationChallengeStore store) { public Task<AuthenticationResult<ChallengeResult>> Handle(BeginPhoneLoginCommand command, CancellationToken cancellationToken) => AuthenticationChallengeHandlerCore.Begin(AuthenticationChallengePurpose.PhoneLogin, store, cancellationToken); }
public sealed class BeginEmailLoginHandler(IAuthenticationChallengeStore store) { public Task<AuthenticationResult<ChallengeResult>> Handle(BeginEmailLoginCommand command, CancellationToken cancellationToken) => AuthenticationChallengeHandlerCore.Begin(AuthenticationChallengePurpose.EmailLogin, store, cancellationToken); }
public sealed class BeginPasskeyLoginHandler(IWebAuthnAuthenticationService webAuthn) { public async Task<AuthenticationResult<ChallengeResult>> Handle(BeginPasskeyLoginCommand command, CancellationToken cancellationToken) { string id = await webAuthn.BeginAsync(command.UserNameOrEmail, cancellationToken); return new AuthenticationResult<ChallengeResult>.Success(new(id, AuthenticationChallengePurpose.PasskeyLogin, DateTimeOffset.UtcNow.AddMinutes(5), 5)); } }

public sealed class CompletePhoneLoginHandler(IAuthenticationCodeVerifier verifier, IAuthenticationRedirectResolver redirects) { public Task<AuthenticationResult<LoginResult>> Handle(CompletePhoneLoginCommand command, CancellationToken cancellationToken) => AuthenticationChallengeHandlerCore.Complete(command.ChallengeId, command.Code, command.ReturnUrl, verifier, redirects, cancellationToken); }
public sealed class CompleteEmailLoginHandler(IAuthenticationCodeVerifier verifier, IAuthenticationRedirectResolver redirects) { public Task<AuthenticationResult<LoginResult>> Handle(CompleteEmailLoginCommand command, CancellationToken cancellationToken) => AuthenticationChallengeHandlerCore.Complete(command.ChallengeId, command.Code, command.ReturnUrl, verifier, redirects, cancellationToken); }
public sealed class CompletePasskeyLoginHandler(IWebAuthnAuthenticationService webAuthn, IAuthenticationSignInService signInService, IAuthenticationRedirectResolver redirects) { public async Task<AuthenticationResult<LoginResult>> Handle(CompletePasskeyLoginCommand command, CancellationToken cancellationToken) { string? userId = await webAuthn.CompleteAsync(command.ChallengeId, command.CredentialJson, cancellationToken); if (userId is null) return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "Passkey authentication failed.")); await signInService.SignInAsync(userId, false, cancellationToken); return new AuthenticationResult<LoginResult>.Success(new(userId, redirects.Resolve(command.ReturnUrl, false), false)); } }

public sealed class LogoutHandler(IAuthenticationSignInService signInService) { public async Task<AuthenticationResult<LogoutResult>> Handle(LogoutCommand command, CancellationToken cancellationToken) { await signInService.SignOutAsync(cancellationToken); return new AuthenticationResult<LogoutResult>.Success(new(true, command.ReturnUrl)); } }
public sealed class GetCurrentAuthenticationSessionHandler(IAuthenticationUserDirectory users) { public async Task<AuthenticationResult<AuthenticationUserDescriptor?>> Handle(GetCurrentAuthenticationSessionQuery query, CancellationToken cancellationToken) => new AuthenticationResult<AuthenticationUserDescriptor?>.Success(await users.GetCurrentAsync(cancellationToken)); }
public sealed class GetLogoutContextHandler(ILogoutContextService contexts) { public async Task<AuthenticationResult<LogoutContext?>> Handle(GetLogoutContextQuery query, CancellationToken cancellationToken) => new AuthenticationResult<LogoutContext?>.Success(await contexts.GetAsync(query.LogoutId, cancellationToken)); }

internal static class AuthenticationChallengeHandlerCore
{
    public static async Task<AuthenticationResult<ChallengeResult>> Begin(AuthenticationChallengePurpose purpose, IAuthenticationChallengeStore store, CancellationToken cancellationToken)
{
    AuthenticationChallenge challenge = new(new AuthenticationChallengeId(Guid.NewGuid().ToString("N")), purpose, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5), 5);
    await store.SaveAsync(challenge, cancellationToken);
    return new AuthenticationResult<ChallengeResult>.Success(new(challenge.Id.Value, challenge.Purpose, challenge.ExpiresAt, challenge.RemainingAttempts));
}

    public static async Task<AuthenticationResult<LoginResult>> Complete(string challengeId, string code, string? returnUrl, IAuthenticationCodeVerifier verifier, IAuthenticationRedirectResolver redirects, CancellationToken cancellationToken)
{
    if (!await verifier.VerifyAsync(challengeId, code, cancellationToken)) return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "Invalid verification code."));
    return new AuthenticationResult<LoginResult>.Success(new(challengeId, redirects.Resolve(returnUrl, false), false));
}

}
