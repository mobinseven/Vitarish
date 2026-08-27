using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
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
    IRequestContext requestContext)
{
    public async Task<AuthenticationResult<LoginResult>> Handle(
        CompletePasswordLoginOtpCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, "A tenant context is required for security auditing."));

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
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureAuditUnavailable));
        }

        AuthenticationResult<LoginResult> result = await ChallengeCompletionHelper.CompleteChallengeAsync(
            command.ChallengeId,
            command.Code,
            command.ReturnUrl,
            AuthenticationChallengePurpose.PasswordLoginOtp,
            store,
            verifier,
            signInService,
            redirects,
            cancellationToken);

        try
        {
            _ = await securityAuditWriter.WriteAsync(CreateAuditEvent(
                result is AuthenticationResult<LoginResult>.Success ? SecurityAuditOutcome.Succeeded : SecurityAuditOutcome.Denied,
                completedKey,
                result is AuthenticationResult<LoginResult>.Success success ? success.Value.UserId : null,
                result is AuthenticationResult<LoginResult>.Failure failure ? failure.Error.Reason.ToString() : null), cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            if (result is AuthenticationResult<LoginResult>.Success)
                await signInService.SignOutAsync(CancellationToken.None);

            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureAuditUnavailable));
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
        ActorId = requestContext.UserId,
        SubjectId = subjectId,
        TenantId = requestContext.TenantId!,
        Resource = AuthenticationSecurityAuditEvents.Resource,
        Operation = AuthenticationSecurityAuditEvents.PasswordLoginOtpOperation,
        OccurredAt = DateTime.UtcNow,
        CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey,
        SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
        RedactedReason = reason,
        IdempotencyKey = idempotencyKey
    };
}
