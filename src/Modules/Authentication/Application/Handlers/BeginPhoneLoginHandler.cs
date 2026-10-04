using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Users.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class BeginPhoneLoginHandler(
    IAuthenticationChallengeStore store,
    IAuthenticationUserDirectory userDirectory,
    IAuthenticationCodeGenerator codeGenerator,
    IPhoneAuthenticationCodeSender phoneSender,
    AuthenticationChallengePolicy policy,
    IAuthenticationAttemptTracker attemptTracker,
    IAuthenticationSecurityPolicyProvider securityPolicyProvider,
    IAuthenticationAttemptKeyFactory attemptKeyFactory)
{
    public async Task<AuthenticationResult<ChallengeResult>> Handle(BeginPhoneLoginCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.PhoneNumber))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "Phone number is required."));
        if (!UserPhoneNumberPolicy.TryNormalize(command.PhoneNumber, out string? phoneNumber))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidInput, UserPhoneNumberPolicy.InvalidPhoneNumberMessage));

        SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
        if (!securityPolicy.Allows(AuthenticationMethod.Phone, PreAuthenticationAction.PhoneLogin))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Phone authentication is not enabled."));

        if (await IsDeniedAsync(attemptKeyFactory.IdentifierCandidates(AuthenticationMethod.Phone, phoneNumber), cancellationToken))
            return new AuthenticationResult<ChallengeResult>.Failure(AttemptDenied(securityPolicy));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationUserDescriptor? user = await userDirectory.FindOrCreateByPhoneNumberAsync(phoneNumber, cancellationToken);
        if (user is null)
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidInput, UserPhoneNumberPolicy.InvalidPhoneNumberMessage));
        if (await IsDeniedAsync(attemptKeyFactory.SubjectCandidates(AuthenticationMethod.Phone, user.UserId), cancellationToken))
            return new AuthenticationResult<ChallengeResult>.Failure(AttemptDenied(securityPolicy));
        AuthenticationChallengePolicy effectivePolicy = policy.WithMaximumAttempts(securityPolicy.FailedAttemptThreshold);

        string tokenPurpose = effectivePolicy.TokenPurpose(AuthenticationChallengePurpose.PhoneLogin);
        string code = await codeGenerator.GenerateAsync(user.UserId, tokenPurpose, cancellationToken);
        AuthenticationChallenge challenge = effectivePolicy.CreatePhoneLoginChallenge(user.UserId, phoneNumber, tokenPurpose, now);
        await store.SaveAsync(challenge, cancellationToken);

        try
        {
            await phoneSender.SendAsync(phoneNumber, code, AuthenticationChallengePurpose.PhoneLogin, cancellationToken);
        }
        catch
        {
            challenge.Cancel();
            await store.ReplaceAsync(challenge, CancellationToken.None);
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, "Failed to send verification code."));
        }

        return new AuthenticationResult<ChallengeResult>.Success(new(challenge.Id.Value, challenge.Purpose, challenge.ExpiresAt, challenge.RemainingAttempts));
    }

    private static AuthenticationFailure AttemptDenied(SecurityAccessPolicy policy) =>
        new(policy.FailedAttemptResponse is FailedAttemptResponse.Lockout ? AuthenticationFailureReason.LockedOut : AuthenticationFailureReason.TooManyAttempts,
            policy.FailedAttemptResponse is FailedAttemptResponse.Lockout ? "Authentication is temporarily locked out." : "Too many authentication attempts.");

    private async Task<bool> IsDeniedAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        foreach (string key in keys)
            if (await attemptTracker.IsDeniedAsync(key, cancellationToken))
                return true;
        return false;
    }
}
