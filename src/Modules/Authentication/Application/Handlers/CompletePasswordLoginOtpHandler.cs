using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class CompletePasswordLoginOtpHandler(
    IAuthenticationChallengeStore store,
    IAuthenticationCodeVerifier verifier,
    IAuthenticationSignInService signInService,
    IAuthenticationRedirectResolver redirects,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext,
    IAuthenticationAttemptTracker attemptTracker,
    IAuthenticationSecurityPolicyProvider securityPolicyProvider,
    IAuthenticationAttemptKeyFactory attemptKeyFactory,
    AuthenticationSourceAttemptGuard sourceAttemptGuard,
    IAuthenticationUtcClock clock,
    IAuthenticationSessionRegistry sessionRegistry,
    IAuthenticationUserDirectory userDirectory)
{
    public async Task<AuthenticationResult<LoginResult>> Handle(
        CompletePasswordLoginOtpCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, "A tenant context is required for security auditing."));

        SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);

        string? knownUserId = null;
        AuthenticationChallenge? challenge = AuthenticationInputValidator.HasText(command.ChallengeId)
            ? await store.GetAsync(AuthenticationChallengeId.Create(command.ChallengeId), cancellationToken)
            : null;
        knownUserId = challenge?.UserId;
        PasswordLoginSecondFactorChannel? channel = challenge is null
            ? PasswordLoginSecondFactorChannel.ForPurpose(AuthenticationChallengePurpose.PasswordLoginOtp)
            : PasswordLoginSecondFactorChannel.FromChallenge(challenge);

        string operationKey = requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        string startedKey = operationKey + AuthenticationSecurityAuditEvents.StartedKeySuffix;
        string completedKey = operationKey + AuthenticationSecurityAuditEvents.CompletedKeySuffix;

        try
        {
            SecurityAuditWriteResult started = await securityAuditWriter.WriteAsync(CreateAuditEvent(
                SecurityAuditOutcome.Started,
                startedKey,
                null), cancellationToken);
            if (started is SecurityAuditWriteResult.AlreadyRecorded)
                return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureDuplicateInProgress));
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the OTP authentication flow when its audit record cannot be stored.
        }

        AuthenticationResult<LoginResult> result;
        if (channel is null)
        {
            result = new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidChallengePurpose, "The challenge is not a password sign-in second factor."));
        }
        else if (!securityPolicy.Allows(channel.Method, channel.PolicyAction))
        {
            result = new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Additional authentication is not enabled."));
        }
        else if (channel.Method is Vitarish.Authentication.Domain.Methods.AuthenticationMethod.Email &&
                 challenge is not null &&
                 challenge.ValidateAttempt(clock.UtcNow, channel.ChallengePurpose) is null &&
                 !channel.DestinationIsCurrentAndConfirmed(await userDirectory.FindByIdAsync(challenge.UserId, cancellationToken)))
        {
            _ = await store.TryConsumeAsync(challenge, CancellationToken.None);
            result = new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Additional authentication is no longer configured."));
        }
        else
        {
            result = await ChallengeCompletionHelper.CompleteChallengeAsync(
                command.ChallengeId,
                command.Code,
                command.ReturnUrl,
                channel.ChallengePurpose,
                store,
                verifier,
                signInService,
                redirects,
                cancellationToken,
                attemptTracker,
                securityPolicy,
                attemptKeyFactory,
                sessionRegistry,
                sourceAttemptGuard,
                onChallengeLoaded: userId => knownUserId = userId);
        }

        try
        {
            _ = await securityAuditWriter.WriteAsync(CreateAuditEvent(
                result is AuthenticationResult<LoginResult>.Success ? SecurityAuditOutcome.Succeeded : SecurityAuditOutcome.Denied,
                completedKey,
                result is AuthenticationResult<LoginResult>.Success success ? success.Value.UserId : knownUserId,
                AuthenticationSecurityAuditEvents.FailureReason(result)), cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the authentication result and established session.
        }
        catch (OperationCanceledException)
        {
            if (result is AuthenticationResult<LoginResult>.Success)
                await signInService.SignOutAsync(CancellationToken.None);
            throw;
        }

        return result;
    }

    private SecurityAuditEvent CreateAuditEvent(string outcome, string idempotencyKey, string? subjectId, string? reason = null) => new()
    {
        EventName = AuthenticationSecurityAuditEvents.PasswordLoginOtp,
        Outcome = outcome,
        ActorId = SecurityAuditSignInActor.Resolve(requestContext.UserId, outcome, subjectId),
        SubjectId = subjectId,
        TenantId = requestContext.TenantId!,
        Resource = AuthenticationSecurityAuditEvents.Resource,
        Operation = AuthenticationSecurityAuditEvents.PasswordLoginOtpOperation,
        OccurredAt = clock.UtcNow,
        CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey,
        SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
        RedactedReason = reason,
        IdempotencyKey = idempotencyKey
    };
}
