using System.Security.Cryptography;
using System.Text;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

/// <summary>Issues a handoff ticket for the signed-in user to continue on <paramref name="TargetHost"/>.</summary>
/// <param name="EndsIssuingSession">
/// When true, the session that asked for the ticket is signed out once the ticket is stored, so the
/// issuing host keeps no session of its own (the shared sign-in host, ADR-0018).
/// </param>
public sealed record IssueTenantHandoffCommand(string UserId, string TargetHost, bool EndsIssuingSession = false, bool LinkIssuingSession = false, string? CorrelationHash = null, string? ReturnPath = null, Guid? PriorCentralSessionId = null, string? ReturnOrigin = null);

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
    AuthenticationAttemptAuditor auditor,
    IAuthenticationSignInService signInService,
    IAuthenticationSessionRegistry sessionRegistry,
    IAuthenticationHostDirectory? hosts = null)
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

        if (hosts?.SignInOrigin is not null && !command.LinkIssuingSession && command.PriorCentralSessionId is null)
        {
            await auditor.RecordAsync(AuthenticationSecurityAuditEvents.TenantHandoffIssue, AuthenticationSecurityAuditEvents.TenantHandoffIssueOperation, command.UserId, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            return null;
        }
        string token = TenantHandoffTokens.Create();
        DateTime now = clock.UtcNow;
        DateTime? authenticatedAt = await sessionRegistry.GetCurrentAuthenticationAtAsync(command.UserId, cancellationToken);
        TenantHandoffTicket ticket = TenantHandoffTicket.Issue(TenantHandoffTokens.Hash(token), command.UserId, command.TargetHost, now, authenticatedAt);
        ticket.CorrelationHash = command.CorrelationHash;
        ticket.ReturnPath = command.ReturnPath;
        ticket.PriorCentralSessionId = command.PriorCentralSessionId;
        ticket.ReturnOrigin = command.ReturnOrigin;
        if (command.LinkIssuingSession)
        {
            string? sessionId = requestContext.User?.FindFirst(AuthenticationSessionClaimTypes.SessionId)?.Value;
            if (!Guid.TryParse(sessionId, out Guid centralSessionId) || string.IsNullOrWhiteSpace(command.CorrelationHash))
                return null;
            ticket.CentralSessionId = centralSessionId;
        }
        await tickets.AddAsync(ticket, now, cancellationToken);
        await auditor.RecordAsync(AuthenticationSecurityAuditEvents.TenantHandoffIssue, AuthenticationSecurityAuditEvents.TenantHandoffIssueOperation, command.UserId, null, cancellationToken);
        // A failed sign-out throws, so the caller never follows a ticket that left this session open.
        if (command.EndsIssuingSession)
            await signInService.SignOutAsync(cancellationToken);
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
    AuthenticationSourceAttemptGuard sourceAttemptGuard,
    IAuthenticationUserDirectory users,
    IAuthenticationRedirectResolver redirects,
    IAuthenticationUtcClock clock,
    AuthenticationAttemptAuditor auditor,
    AuthenticationSessionLinkContext? sessionLink = null,
    IAuthenticationBrowserCorrelation? correlation = null,
    IAuthenticationPreviousSessionCookie? previousCookie = null,
    IAuthenticationHostDirectory? hostDirectory = null)
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

        if (hostDirectory?.SignInOrigin is not null && ticket.CorrelationHash is null && ticket.PriorCentralSessionId is null)
            return await RefuseAsync(ticket.UserId, AuthenticationFailureReason.InvalidCredential, RefusalMessage, SecurityAuditReasonCatalog.AuthenticationFailed, cancellationToken);

        if (ticket.CorrelationHash is not null &&
            (correlation?.Read(ticket.CorrelationHash) is not string browserCorrelation || !TenantHandoffTokens.IsWellFormed(browserCorrelation) ||
             !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(ticket.CorrelationHash), Encoding.ASCII.GetBytes(TenantHandoffTokens.Hash(browserCorrelation)))))
            return await RefuseAsync(ticket.UserId, AuthenticationFailureReason.InvalidCredential, RefusalMessage, SecurityAuditReasonCatalog.AuthenticationFailed, cancellationToken);

        if (ticket.CorrelationHash is not null) correlation?.Clear(ticket.CorrelationHash);

        if (ticket.PriorCentralSessionId is Guid prior &&
            (previousCookie?.Read(ticket.UserId) != prior || hostDirectory is null ||
             !await hostDirectory.IsCentralHostAsync(cancellationToken) || ticket.ReturnOrigin is null ||
             !await hostDirectory.CanOpenAsync(ticket.ReturnOrigin, ticket.UserId, cancellationToken)))
            return await RefuseAsync(ticket.UserId, AuthenticationFailureReason.InvalidCredential, RefusalMessage, SecurityAuditReasonCatalog.AuthenticationFailed, cancellationToken);

        if (await sourceAttemptGuard.IsBlockedAsync(cancellationToken) || await IsDeniedAsync(policy, ticket.UserId, cancellationToken))
        {
            AuthenticationFailure denied = policy.FailedAttemptResponse is FailedAttemptResponse.Lockout
                ? new(AuthenticationFailureReason.LockedOut, "Authentication is temporarily locked out.")
                : new(AuthenticationFailureReason.TooManyAttempts, "Too many authentication attempts.");
            return await RefuseAsync(ticket.UserId, denied.Reason, denied.Message, SecurityAuditReasonCatalog.AuthenticationFailed, cancellationToken);
        }

        if (!await tickets.TryMarkRedeemedAsync(ticket, now, cancellationToken))
            return await RefuseAsync(ticket.UserId, AuthenticationFailureReason.InvalidCredential, RefusalMessage, SecurityAuditReasonCatalog.AuthenticationFailed, cancellationToken);

        if (ticket.CentralSessionId is not null && sessionLink is null)
            return await RefuseAsync(ticket.UserId, AuthenticationFailureReason.ProviderFailure, RefusalMessage, SecurityAuditReasonCatalog.AuthenticationFailed, cancellationToken);
        if (sessionLink is not null)
            sessionLink.CentralSessionId = ticket.CentralSessionId;
        AuthenticationSessionSignInResult session;
        try
        {
            session = await signInService.SignInFromTenantHandoffAsync(ticket.UserId, ticket.AuthenticatedAt, cancellationToken);
        }
        finally
        {
            if (sessionLink is not null)
                sessionLink.CentralSessionId = null;
        }
        if (!session.Succeeded)
            return await RefuseAsync(ticket.UserId, session.Failure!.Reason, session.Failure.Message, SecurityAuditReasonCatalog.AuthenticationFailed, cancellationToken);

        await auditor.RecordAsync(AuthenticationSecurityAuditEvents.TenantHandoffRedeem, AuthenticationSecurityAuditEvents.TenantHandoffRedeemOperation, ticket.UserId, null, cancellationToken);
        bool requiresProfileCompletion = (await users.FindByIdAsync(ticket.UserId, cancellationToken))?.RequiresProfileCompletion ?? false;
        string returnUrl = await redirects.ResolveAsync(ticket.ReturnPath, requiresProfileCompletion, ticket.UserId, cancellationToken);
        if (ticket.PriorCentralSessionId is not null && ticket.ReturnOrigin is not null)
            returnUrl = ticket.ReturnOrigin + Vitarish.Authentication.Domain.ReturnUrls.AuthenticationBrowserPaths.Login;
        return new AuthenticationResult<LoginResult>.Success(new LoginResult(ticket.UserId, returnUrl, requiresProfileCompletion));
    }

    // Checks the previous attempt-key version too, so a denial survives key rotation.
    private async Task<bool> IsDeniedAsync(SecurityAccessPolicy policy, string userId, CancellationToken cancellationToken)
    {
        foreach (AuthenticationMethod mechanism in policy.EnabledAuthenticationMechanisms)
            foreach (string key in attemptKeyFactory.SubjectCandidates(mechanism, userId))
                if (await attemptTracker.IsDeniedAsync(key, cancellationToken))
                    return true;
        return false;
    }

    private async Task<AuthenticationResult<LoginResult>> RefuseAsync(string? subjectId, AuthenticationFailureReason reason, string message, string auditReason, CancellationToken cancellationToken)
    {
        await auditor.RecordAsync(AuthenticationSecurityAuditEvents.TenantHandoffRedeem, AuthenticationSecurityAuditEvents.TenantHandoffRedeemOperation, subjectId, auditReason, cancellationToken);
        return new AuthenticationResult<LoginResult>.Failure(new AuthenticationFailure(reason, message));
    }
}
