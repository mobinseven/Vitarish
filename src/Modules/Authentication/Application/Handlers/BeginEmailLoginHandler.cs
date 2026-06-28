using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class BeginEmailLoginHandler(
    IAuthenticationChallengeStore store,
    IAuthenticationUserDirectory userDirectory,
    IAuthenticationCodeGenerator codeGenerator,
    IEmailAuthenticationCodeSender emailSender,
    AuthenticationChallengePolicy policy)
{
    public async Task<AuthenticationResult<ChallengeResult>> Handle(BeginEmailLoginCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.Email))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "Email is required."));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationUserDescriptor? user = await userDirectory.FindByEmailAsync(command.Email, cancellationToken);
        if (user is null)
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "No account found for this email."));

        string code = await codeGenerator.GenerateAsync(user.UserId, policy.TokenPurpose(AuthenticationChallengePurpose.EmailLogin), cancellationToken);
        AuthenticationChallenge challenge = policy.CreateEmailLoginChallenge(user.UserId, command.Email, policy.TokenPurpose(AuthenticationChallengePurpose.EmailLogin), now);
        await store.SaveAsync(challenge, cancellationToken);

        try
        {
            await emailSender.SendAsync(command.Email, code, AuthenticationChallengePurpose.EmailLogin, cancellationToken);
        }
        catch
        {
            challenge.Cancel();
            await store.ReplaceAsync(challenge, CancellationToken.None);
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, "Failed to send verification code."));
        }

        return new AuthenticationResult<ChallengeResult>.Success(new(challenge.Id.Value, challenge.Purpose, challenge.ExpiresAt, challenge.RemainingAttempts));
    }
}
