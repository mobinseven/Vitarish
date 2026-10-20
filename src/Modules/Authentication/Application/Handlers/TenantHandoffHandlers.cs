using System.Security.Cryptography;
using System.Text;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

/// <summary>Issues a handoff ticket for the signed-in user to continue on <paramref name="TargetHost"/>.</summary>
public sealed record IssueTenantHandoffCommand(string UserId, string TargetHost);

public sealed record RedeemTenantHandoffCommand(string Token);

/// <summary>Raw handoff tokens: 32 random bytes, base64url without padding; only their hash is stored.</summary>
public static class TenantHandoffTokens
{
    public const int TokenLength = 43;

    public static string Create() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static bool IsWellFormed(string? token) =>
        token is { Length: TokenLength } && token.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    public static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(token)));
}

public sealed class IssueTenantHandoffHandler(
    ITenantHandoffTicketStore tickets,
    IAuthenticationSecurityPolicyProvider securityPolicyProvider,
    IRequestContext requestContext,
    IAuthenticationUtcClock clock,
    TenantHandoffAuditor auditor)
{
    /// <returns>The raw token, shown once, or null when the handoff is not allowed.</returns>
    public async Task<string?> Handle(IssueTenantHandoffCommand command, CancellationToken cancellationToken)
    {
        // A user hands off only their own session.
        if (string.IsNullOrWhiteSpace(command.UserId) || !string.Equals(requestContext.UserId, command.UserId, StringComparison.Ordinal))
        {
            await auditor.RecordAsync(AuthenticationSecurityAuditEvents.TenantHandoffIssue, AuthenticationSecurityAuditEvents.TenantHandoffIssueOperation, command.UserId, SecurityAuditReasonCatalog.Unauthorized, cancellationToken);
            return null;
        }

        SecurityAccessPolicy policy = await securityPolicyProvider.GetAsync(cancellationToken);
        if (!policy.PermittedPreAuthenticationActions.Contains(PreAuthenticationAction.TenantHandoff))
        {
            await auditor.RecordAsync(AuthenticationSecurityAuditEvents.TenantHandoffIssue, AuthenticationSecurityAuditEvents.TenantHandoffIssueOperation, command.UserId, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            return null;
        }

        string token = TenantHandoffTokens.Create();
        DateTime now = clock.UtcNow;
        await tickets.AddAsync(TenantHandoffTicket.Issue(TenantHandoffTokens.Hash(token), command.UserId, command.TargetHost, now), now, cancellationToken);
        await auditor.RecordAsync(AuthenticationSecurityAuditEvents.TenantHandoffIssue, AuthenticationSecurityAuditEvents.TenantHandoffIssueOperation, command.UserId, null, cancellationToken);
        return token;
    }
}

/// <summary>
/// Redeems a handoff ticket on the host it was issued for and starts a normal session there
/// through the sign-in service. Unknown, malformed, expired, replayed, and wrong-host tickets
/// get the same refusal, and the user falls back to normal sign-in. A ticket carries no
/// guessable secret or user identifier, so per-IP rate limiting bounds guessing; a user denied
/// by the failed-attempt policy under any enabled mechanism cannot use one to bypass it.
/// </summary>
public sealed class RedeemTenantHandoffHandler(
    ITenantHandoffTicketStore tickets,
    IAuthenticationSecurityPolicyProvider securityPolicyProvider,
    IAuthenticationSignInService signInService,
    IClientInterfaceHostAccessor clientInterfaceHosts,
    IAuthenticationAttemptTracker attemptTracker,
    IAuthenticationAttemptKeyFactory attemptKeyFactory,
    IAuthenticationUtcClock clock,
    TenantHandoffAuditor auditor)
{
    public const string RefusalMessage = "The sign-in link is invalid or has expired. Sign in to continue.";

    public async Task<AuthenticationResult<LoginResult>> Handle(RedeemTenantHandoffCommand command, CancellationToken cancellationToken)
    {
        SecurityAccessPolicy policy = await securityPolicyProvider.GetAsync(cancellationToken);
        if (!policy.PermittedPreAuthenticationActions.Contains(PreAuthenticationAction.TenantHandoff))
            return await RefuseAsync(null, AuthenticationFailureReason.NotAllowed, "Handoff sign-in is not enabled.", SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
        if (!TenantHandoffTokens.IsWellFormed(command.Token))
            return await RefuseAsync(null, AuthenticationFailureReason.InvalidCredential, RefusalMessage, SecurityAuditReasonCatalog.AuthenticationFailed, cancellationToken);

        DateTime now = clock.UtcNow;
        string? host = clientInterfaceHosts.ClientInterfaceHost;
        TenantHandoffTicket? ticket = await tickets.FindByTokenHashAsync(TenantHandoffTokens.Hash(command.Token), cancellationToken);
        if (ticket is null || host is null || !ticket.CanBeRedeemedOn(host, now))
            return await RefuseAsync(ticket?.UserId, AuthenticationFailureReason.InvalidCredential, RefusalMessage, SecurityAuditReasonCatalog.AuthenticationFailed, cancellationToken);

        foreach (AuthenticationMethod mechanism in policy.EnabledAuthenticationMechanisms)
        {
            if (await attemptTracker.IsDeniedAsync(attemptKeyFactory.Subject(mechanism, ticket.UserId), cancellationToken))
                return await RefuseAsync(ticket.UserId, AuthenticationFailureReason.LockedOut, "Authentication is temporarily locked out.", SecurityAuditReasonCatalog.AuthenticationFailed, cancellationToken);
        }

        if (!await tickets.TryMarkRedeemedAsync(ticket, now, cancellationToken))
            return await RefuseAsync(ticket.UserId, AuthenticationFailureReason.InvalidCredential, RefusalMessage, SecurityAuditReasonCatalog.AuthenticationFailed, cancellationToken);

        AuthenticationSessionSignInResult session = await signInService.SignInAsync(ticket.UserId, cancellationToken);
        if (!session.Succeeded)
            return await RefuseAsync(ticket.UserId, session.Failure!.Reason, session.Failure.Message, SecurityAuditReasonCatalog.AuthenticationFailed, cancellationToken);

        await auditor.RecordAsync(AuthenticationSecurityAuditEvents.TenantHandoffRedeem, AuthenticationSecurityAuditEvents.TenantHandoffRedeemOperation, ticket.UserId, null, cancellationToken);
        return new AuthenticationResult<LoginResult>.Success(new LoginResult(ticket.UserId, "/", RequiresProfileCompletion: false));
    }

    private async Task<AuthenticationResult<LoginResult>> RefuseAsync(string? subjectId, AuthenticationFailureReason reason, string message, string auditReason, CancellationToken cancellationToken)
    {
        await auditor.RecordAsync(AuthenticationSecurityAuditEvents.TenantHandoffRedeem, AuthenticationSecurityAuditEvents.TenantHandoffRedeemOperation, subjectId, auditReason, cancellationToken);
        return new AuthenticationResult<LoginResult>.Failure(new AuthenticationFailure(reason, message));
    }
}

public sealed class TenantHandoffAuditor(ISecurityAuditWriter auditWriter, IRequestContext requestContext, IAuthenticationUtcClock clock)
{
    /// <param name="failureReason">Null when the step succeeded.</param>
    public async Task RecordAsync(string eventName, string operation, string? subjectId, string? failureReason, CancellationToken cancellationToken)
    {
        string requestKey = requestContext.RequestId ?? requestContext.TraceId ?? Guid.NewGuid().ToString("N");
        try
        {
            _ = await auditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = eventName,
                EventCategory = SecurityAuditEventCatalog.AuthenticationCategory,
                Outcome = failureReason is null ? SecurityAuditOutcome.Succeeded : SecurityAuditOutcome.Denied,
                ActorId = requestContext.UserId,
                SubjectId = subjectId,
                TenantId = requestContext.TenantId ?? SecurityAuditScopeCatalog.PlatformScopeId,
                Resource = AuthenticationSecurityAuditEvents.Resource,
                Operation = operation,
                OccurredAt = clock.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestKey,
                SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
                RedactedReason = failureReason,
                IdempotencyKey = requestKey + AuthenticationSecurityAuditEvents.AuthenticationAttemptIdempotencySuffix + operation
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the authentication result when the audit store is unavailable.
        }
    }
}
