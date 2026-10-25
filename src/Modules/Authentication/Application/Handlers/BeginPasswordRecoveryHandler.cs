using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Users.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class BeginPasswordRecoveryHandler(
    IAuthenticationChallengeStore challengeStore,
    IAuthenticationUserDirectory userDirectory,
    IAuthenticationCodeGenerator codeGenerator,
    IPasswordRecoveryCodeDispatcher codeDispatcher,
    IUserAccountUpdater accountUpdater,
    IAuthenticationAttemptTracker attemptTracker,
    IAuthenticationAttemptKeyFactory attemptKeyFactory,
    IAuthenticationRecoveryDeliveryFailureRecorder failureRecorder,
    AuthenticationChallengePolicy challengePolicy,
    IAuthenticationSecurityPolicyProvider securityPolicyProvider,
    AuthenticationSourceAttemptGuard sourceAttemptGuard,
    TimeProvider timeProvider)
{
    public async Task<AuthenticationResult<ChallengeResult>> Handle(BeginPasswordRecoveryCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.Contact) || command.Channel is not (PasswordRecoveryChannel.Email or PasswordRecoveryChannel.Sms))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "A supported recovery channel and contact are required."));

        // An SMS contact that cannot be converted behaves exactly like an unknown
        // number: no directory lookup, no real challenge, the same public response.
        string contact = command.Contact.Trim();
        bool smsContactUsable = true;
        if (command.Channel is PasswordRecoveryChannel.Sms)
        {
            if (UserPhoneNumberPolicy.TryNormalize(command.Contact, out string? normalizedPhone))
                contact = normalizedPhone;
            else
                smsContactUsable = false;
        }

        long startedAt = timeProvider.GetTimestamp();
        SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
        if (!securityPolicy.PermittedPreAuthenticationActions.Contains(PreAuthenticationAction.PasswordRecovery))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Password recovery is not enabled."));

        AuthenticationMethod method = MethodFor(command.Channel);
        IReadOnlyList<string> identifierKeys = attemptKeyFactory.IdentifierCandidates(method, contact);
        bool denied = await sourceAttemptGuard.IsBlockedAsync(cancellationToken) || await IsDeniedAsync(identifierKeys, cancellationToken);

        AuthenticationUserDescriptor? user = null;
        if (!denied && smsContactUsable)
        {
            user = command.Channel switch
            {
                PasswordRecoveryChannel.Email => await userDirectory.FindByEmailAsync(contact, cancellationToken),
                PasswordRecoveryChannel.Sms => await userDirectory.FindByPhoneNumberAsync(contact, cancellationToken),
                _ => null
            };

            if (user is not null)
                denied = await IsDeniedAsync(attemptKeyFactory.SubjectCandidates(method, user.UserId), cancellationToken);
        }

        bool eligible = !denied && user is not null && await accountUpdater.UserHasPasswordAsync(user.UserId, cancellationToken);
        string? registeredDestination = user is null
            ? null
            : command.Channel is PasswordRecoveryChannel.Email ? user.Email : user.PhoneNumber;
        eligible = eligible && !string.IsNullOrWhiteSpace(registeredDestination);

        string userId = eligible ? user!.UserId : Guid.NewGuid().ToString("N");
        string destination = eligible ? registeredDestination! : string.Empty;
        AuthenticationChallengePolicy effectivePolicy = challengePolicy.WithMaximumAttempts(securityPolicy.FailedAttemptThreshold);
        DateTimeOffset now = timeProvider.GetUtcNow();
        AuthenticationChallenge challenge = command.Channel is PasswordRecoveryChannel.Email
            ? effectivePolicy.CreateEmailPasswordRecoveryChallenge(userId, destination, now)
            : effectivePolicy.CreatePhonePasswordRecoveryChallenge(userId, destination, now);
        await challengeStore.SaveAsync(challenge, cancellationToken);

        if (eligible)
        {
            try
            {
                string code = await codeGenerator.GenerateAsync(userId, challenge.TokenPurpose, cancellationToken);
                await codeDispatcher.DispatchAsync(challenge, code, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                challenge.Cancel();
                await challengeStore.ReplaceAsync(challenge, CancellationToken.None);
                await failureRecorder.RecordAsync(command.Channel, exception.GetType().Name, CancellationToken.None);
            }
        }

        TimeSpan remainingResponseDelay = AuthenticationChallengePolicy.PasswordRecoveryMinimumResponseDuration - timeProvider.GetElapsedTime(startedAt);
        if (remainingResponseDelay > TimeSpan.Zero)
            await Task.Delay(remainingResponseDelay, timeProvider, cancellationToken);

        return new AuthenticationResult<ChallengeResult>.Success(
            new(challenge.Id.Value, challenge.Purpose, challenge.ExpiresAt, challenge.RemainingAttempts));
    }

    private async Task<bool> IsDeniedAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        foreach (string key in keys)
            if (await attemptTracker.IsDeniedAsync(key, cancellationToken))
                return true;
        return false;
    }

    private static AuthenticationMethod MethodFor(PasswordRecoveryChannel channel) => channel switch
    {
        PasswordRecoveryChannel.Email => AuthenticationMethod.Email,
        PasswordRecoveryChannel.Sms => AuthenticationMethod.Phone,
        _ => throw new ArgumentOutOfRangeException(nameof(channel))
    };
}
