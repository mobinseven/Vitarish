using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class BeginEmailLoginHandler(
    IAuthenticationChallengeStore store,
    IAuthenticationUserDirectory userDirectory,
    IAuthenticationCodeGenerator codeGenerator,
    IEmailAuthenticationCodeSender emailSender,
    AuthenticationChallengePolicy policy,
    IAuthenticationAttemptTracker attemptTracker,
    IAuthenticationSecurityPolicyProvider securityPolicyProvider,
    IAuthenticationAttemptKeyFactory attemptKeyFactory)
{
    public async Task<AuthenticationResult<ChallengeResult>> Handle(BeginEmailLoginCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.Email))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "Email is required."));

        SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
        if (!securityPolicy.Allows(AuthenticationMethod.Email, PreAuthenticationAction.EmailLogin))
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Email authentication is not enabled."));

        if (await IsDeniedAsync(attemptKeyFactory.IdentifierCandidates(AuthenticationMethod.Email, command.Email), cancellationToken))
            return new AuthenticationResult<ChallengeResult>.Failure(AttemptDenied(securityPolicy));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationUserDescriptor user = await userDirectory.FindOrCreateByEmailAsync(command.Email, cancellationToken);
        if (await IsDeniedAsync(attemptKeyFactory.SubjectCandidates(AuthenticationMethod.Email, user.UserId), cancellationToken))
            return new AuthenticationResult<ChallengeResult>.Failure(AttemptDenied(securityPolicy));
        AuthenticationChallengePolicy effectivePolicy = policy.WithMaximumAttempts(securityPolicy.FailedAttemptThreshold);

        AuthenticationChallenge challenge = effectivePolicy.CreateEmailLoginChallenge(user.UserId, command.Email, now);
        string code = await codeGenerator.GenerateAsync(user.UserId, challenge.TokenPurpose, cancellationToken);
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
