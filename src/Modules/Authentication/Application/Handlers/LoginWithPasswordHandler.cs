using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class LoginWithPasswordHandler(
    IAuthenticationSignInService signInService,
    IAuthenticationRedirectResolver redirects,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext)
{
    public async Task<AuthenticationResult<LoginResult>> Handle(LoginWithPasswordCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.UserNameOrEmail) || !AuthenticationInputValidator.HasText(command.Password))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "Login and password are required."));

        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, "A tenant context is required for security auditing."));

        string operationKey = command.IdempotencyKey ?? requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        string startedKey = operationKey + AuthenticationSecurityAuditEvents.StartedKeySuffix;
        string completedKey = operationKey + AuthenticationSecurityAuditEvents.CompletedKeySuffix;
        try
        {
            SecurityAuditWriteResult started = await securityAuditWriter.WriteAsync(CreateAuditEvent(
                SecurityAuditOutcome.Started,
                startedKey,
                null,
                null), cancellationToken);
            if (started is SecurityAuditWriteResult.AlreadyRecorded)
                return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureDuplicateInProgress));
        }
        catch (SecurityAuditStoreException)
        {
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureAuditUnavailable));
        }

        PasswordSignInResult signInResult = await signInService.PasswordSignInAsync(command.UserNameOrEmail, command.Password, command.RememberMe, cancellationToken);

        AuthenticationResult<LoginResult> result = signInResult.Status switch
        {
            PasswordSignInStatus.Succeeded =>
                new AuthenticationResult<LoginResult>.Success(new(
                    signInResult.UserId!,
                    await redirects.ResolveAsync(command.ReturnUrl, signInResult.RequiresProfileCompletion, signInResult.UserId, cancellationToken),
                    signInResult.RequiresProfileCompletion)),
            PasswordSignInStatus.InvalidCredentials or PasswordSignInStatus.UserNotFound =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "Invalid credentials.")),
            PasswordSignInStatus.LockedOut =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.LockedOut, "Account is locked out.")),
            PasswordSignInStatus.NotAllowed =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Sign in is not allowed.")),
            PasswordSignInStatus.RequiresTwoFactor =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.AdditionalStepRequired, "Additional authentication required.")),
            PasswordSignInStatus.ProviderFailure =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, "Authentication provider failure.")),
            _ => new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "Invalid credentials."))
        };

        try
        {
            _ = await securityAuditWriter.WriteAsync(CreateAuditEvent(
                result is AuthenticationResult<LoginResult>.Success ? SecurityAuditOutcome.Succeeded : SecurityAuditOutcome.Denied,
                completedKey,
                signInResult.UserId,
                result is AuthenticationResult<LoginResult>.Failure failure ? failure.Error.Reason.ToString() : null), cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureAuditUnavailable));
        }

        return result;
    }

    private SecurityAuditEvent CreateAuditEvent(string outcome, string idempotencyKey, string? subjectId, string? reason) => new()
    {
        EventName = AuthenticationSecurityAuditEvents.PasswordLogin,
        Outcome = outcome,
        ActorId = requestContext.UserId,
        SubjectId = subjectId,
        TenantId = requestContext.TenantId!,
        Resource = "authentication",
        Operation = "password.login",
        OccurredAt = DateTime.UtcNow,
        CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey,
        SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
        RedactedReason = reason,
        IdempotencyKey = idempotencyKey
    };
}
