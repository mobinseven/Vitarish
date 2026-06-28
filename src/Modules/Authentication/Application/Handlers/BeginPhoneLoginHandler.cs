using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class BeginPhoneLoginHandler(
    IAuthenticationChallengeStore store,
    IAuthenticationUserDirectory userDirectory,
    IAuthenticationCodeGenerator codeGenerator,
    IPhoneAuthenticationCodeSender phoneSender,
    AuthenticationChallengePolicy policy)
{
    public async Task<AuthenticationResult<ChallengeResult>> Handle(BeginPhoneLoginCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.PhoneNumber))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "Phone number is required."));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationUserDescriptor? user = await userDirectory.FindByPhoneNumberAsync(command.PhoneNumber, cancellationToken);
        if (user is null)
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "No account found for this phone number."));

        string code = await codeGenerator.GenerateAsync(user.UserId, policy.TokenPurpose(AuthenticationChallengePurpose.PhoneLogin), cancellationToken);
        AuthenticationChallenge challenge = policy.CreatePhoneLoginChallenge(user.UserId, command.PhoneNumber, policy.TokenPurpose(AuthenticationChallengePurpose.PhoneLogin), now);
        await store.SaveAsync(challenge, cancellationToken);

        try
        {
            await phoneSender.SendAsync(command.PhoneNumber, code, AuthenticationChallengePurpose.PhoneLogin, cancellationToken);
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
