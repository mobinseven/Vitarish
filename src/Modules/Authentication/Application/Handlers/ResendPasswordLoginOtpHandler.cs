using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class ResendPasswordLoginOtpHandler(
    IAuthenticationChallengeStore challengeStore,
    IAuthenticationUserDirectory userDirectory,
    IAuthenticationCodeGenerator codeGenerator,
    IPhoneAuthenticationCodeSender phoneSender,
    IEmailAuthenticationCodeSender emailSender,
    IAuthenticationAttemptTracker attemptTracker,
    IAuthenticationSecurityPolicyProvider securityPolicyProvider,
    IAuthenticationAttemptKeyFactory attemptKeyFactory,
    AuthenticationSourceAttemptGuard sourceAttemptGuard,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext,
    IAuthenticationUtcClock clock)
{
    public async Task<AuthenticationResult<ChallengeResult>> Handle(
        ResendPasswordLoginOtpCommand command,
        CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.ChallengeId))
            return Failure(AuthenticationFailureReason.InvalidInput, "Challenge ID is required.");
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
            return Failure(AuthenticationFailureReason.ProviderFailure, "A tenant context is required for security auditing.");

        string operationKey = requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        string startedKey = operationKey + AuthenticationSecurityAuditEvents.StartedKeySuffix;
        string completedKey = operationKey + AuthenticationSecurityAuditEvents.CompletedKeySuffix;
        try
        {
            SecurityAuditWriteResult started = await securityAuditWriter.WriteAsync(
                CreateAuditEvent(SecurityAuditOutcome.Started, startedKey, null),
                cancellationToken);
            if (started is SecurityAuditWriteResult.AlreadyRecorded)
                return Failure(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureDuplicateInProgress);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 keeps the initiating resend outcome when an audit record cannot be stored.
        }

        SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
        AuthenticationChallenge? challenge = await challengeStore.GetAsync(
            AuthenticationChallengeId.Create(command.ChallengeId),
            cancellationToken);
        string? knownUserId = challenge?.UserId;
        AuthenticationResult<ChallengeResult> result = await ResendAsync(challenge, securityPolicy, cancellationToken);

        try
        {
            _ = await securityAuditWriter.WriteAsync(CreateAuditEvent(
                result is AuthenticationResult<ChallengeResult>.Success ? SecurityAuditOutcome.Succeeded : SecurityAuditOutcome.Denied,
                completedKey,
                knownUserId,
                result is AuthenticationResult<ChallengeResult>.Failure ? AuthenticationSecurityAuditEvents.AuthenticationFailed : null),
                cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the delivery result when the audit store is unavailable.
        }

        return result;
    }

    private async Task<AuthenticationResult<ChallengeResult>> ResendAsync(
        AuthenticationChallenge? challenge,
        SecurityAccessPolicy securityPolicy,
        CancellationToken cancellationToken)
    {
        if (challenge is null)
            return Failure(AuthenticationFailureReason.InvalidCredentials, "Invalid verification code.");

        PasswordLoginSecondFactorChannel? channel = PasswordLoginSecondFactorChannel.FromChallenge(challenge);
        if (channel is null)
            return Failure(AuthenticationFailureReason.InvalidChallengePurpose, "The challenge is not a password sign-in second factor.");
        if (!securityPolicy.Allows(channel.Method, channel.PolicyAction))
            return Failure(AuthenticationFailureReason.NotAllowed, "Additional authentication is not enabled.");

        if (!channel.DestinationIsCurrentAndConfirmed(await userDirectory.FindByIdAsync(challenge.UserId, cancellationToken)))
        {
            _ = await challengeStore.TryConsumeAsync(challenge, CancellationToken.None);
            return Failure(AuthenticationFailureReason.NotAllowed, "Additional authentication is no longer configured.");
        }

        IReadOnlyList<string> attemptKeys = attemptKeyFactory.ChallengeCandidates(challenge.Purpose, challenge.UserId);
        if (await sourceAttemptGuard.IsBlockedAsync(cancellationToken) || await IsDeniedAsync(attemptKeys, cancellationToken))
            return AttemptDenied(securityPolicy);

        AuthenticationFailureReason? stateFailure = challenge.ValidateAttempt(clock.UtcNow, challenge.Purpose);
        if (stateFailure is not null)
            return Failure(stateFailure.Value, MessageFor(stateFailure.Value));

        if (!await challengeStore.TryReserveAttemptAsync(challenge, cancellationToken))
            return Failure(AuthenticationFailureReason.ChallengeAttemptsExhausted, "Too many failed attempts.");

        try
        {
            string code = await codeGenerator.GenerateAsync(challenge.UserId, challenge.TokenPurpose, cancellationToken);
            if (channel.Method is AuthenticationMethod.Phone)
                await phoneSender.SendAsync(challenge.Destination, code, challenge.Purpose, cancellationToken);
            else
                await emailSender.SendAsync(challenge.Destination, code, challenge.Purpose, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Failure(AuthenticationFailureReason.ProviderFailure, "Additional authentication is temporarily unavailable.");
        }
        finally
        {
            await challengeStore.ReleaseAttemptAsync(challenge, CancellationToken.None);
        }

        return new AuthenticationResult<ChallengeResult>.Success(new(
            challenge.Id.Value,
            challenge.Purpose,
            challenge.ExpiresAt,
            challenge.RemainingAttempts));
    }

    private async Task<bool> IsDeniedAsync(IReadOnlyList<string> attemptKeys, CancellationToken cancellationToken)
    {
        foreach (string key in attemptKeys)
            if (await attemptTracker.IsDeniedAsync(key, cancellationToken))
                return true;
        return false;
    }

    private static AuthenticationResult<ChallengeResult>.Failure AttemptDenied(SecurityAccessPolicy policy) =>
        Failure(
            policy.FailedAttemptResponse is FailedAttemptResponse.Lockout ? AuthenticationFailureReason.LockedOut : AuthenticationFailureReason.TooManyAttempts,
            policy.FailedAttemptResponse is FailedAttemptResponse.Lockout ? "Authentication is temporarily locked out." : "Too many authentication attempts.");

    private static AuthenticationResult<ChallengeResult>.Failure Failure(AuthenticationFailureReason reason, string message) =>
        new(new AuthenticationFailure(reason, message));

    private static string MessageFor(AuthenticationFailureReason reason) => reason switch
    {
        AuthenticationFailureReason.ChallengeExpired => "Challenge has expired.",
        AuthenticationFailureReason.ChallengeAlreadyUsed => "Challenge has already been used.",
        AuthenticationFailureReason.ChallengeAttemptsExhausted => "Too many failed attempts.",
        _ => "The challenge cannot be resent."
    };

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
