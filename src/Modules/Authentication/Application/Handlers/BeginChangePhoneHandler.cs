using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class BeginChangePhoneHandler(
    IAuthenticationChallengeStore store,
    IAuthenticationUserDirectory userDirectory,
    IAuthenticationCodeGenerator codeGenerator,
    IPhoneAuthenticationCodeSender phoneSender,
    AuthenticationChallengePolicy policy)
{
    public async Task<AuthenticationResult<ChallengeResult>> Handle(BeginChangePhoneCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.UserId))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "User ID is required."));

        if (!AuthenticationInputValidator.HasText(command.PhoneNumber))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "Phone number is required."));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string code = await codeGenerator.GenerateAsync(command.UserId, policy.TokenPurpose(AuthenticationChallengePurpose.PhoneChange), cancellationToken);
        AuthenticationChallenge challenge = policy.CreatePhoneChangeChallenge(command.UserId, command.PhoneNumber, policy.TokenPurpose(AuthenticationChallengePurpose.PhoneChange), now);
        await store.SaveAsync(challenge, cancellationToken);

        try
        {
            await phoneSender.SendAsync(command.PhoneNumber, code, AuthenticationChallengePurpose.PhoneChange, cancellationToken);
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
