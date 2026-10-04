using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Users.Domain.Security;

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
        if (!UserPhoneNumberPolicy.TryNormalize(command.PhoneNumber, out string? phoneNumber))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidInput, UserPhoneNumberPolicy.InvalidPhoneNumberMessage));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationChallenge challenge = policy.CreatePhoneChangeChallenge(command.UserId, phoneNumber, now);
        string code = await codeGenerator.GenerateAsync(command.UserId, challenge.TokenPurpose, cancellationToken);
        await store.SaveAsync(challenge, cancellationToken);

        try
        {
            await phoneSender.SendAsync(phoneNumber, code, AuthenticationChallengePurpose.PhoneChange, cancellationToken);
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
