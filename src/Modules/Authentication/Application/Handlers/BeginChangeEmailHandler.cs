using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class BeginChangeEmailHandler(
    IAuthenticationChallengeStore store,
    IAuthenticationUserDirectory userDirectory,
    IAuthenticationCodeGenerator codeGenerator,
    IEmailAuthenticationCodeSender emailSender,
    AuthenticationChallengePolicy policy)
{
    public async Task<AuthenticationResult<ChallengeResult>> Handle(BeginChangeEmailCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.UserId))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "User ID is required."));

        if (!AuthenticationInputValidator.HasText(command.Email))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "Email is required."));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string code = await codeGenerator.GenerateAsync(command.UserId, policy.TokenPurpose(AuthenticationChallengePurpose.EmailChange), cancellationToken);
        AuthenticationChallenge challenge = policy.CreateEmailChangeChallenge(command.UserId, command.Email, policy.TokenPurpose(AuthenticationChallengePurpose.EmailChange), now);
        await store.SaveAsync(challenge, cancellationToken);

        try
        {
            await emailSender.SendAsync(command.Email, code, AuthenticationChallengePurpose.EmailChange, cancellationToken);
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
