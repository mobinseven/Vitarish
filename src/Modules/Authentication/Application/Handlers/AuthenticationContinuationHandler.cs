using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Domain.ReturnUrls;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class AuthenticationContinuationHandler(
    IAuthenticationContinuationStore store,
    IAuthenticationBrowserCorrelation correlation,
    IAuthenticationHostDirectory hosts,
    IAuthenticationUtcClock clock,
    IRequestContext requestContext,
    IssueTenantHandoffHandler handoffs,
    AuthenticationAttemptAuditor auditor,
    IAuthenticationRedirectResolver redirects,
    IAuthenticationUserDirectory users)
{
    public async Task<string?> BeginAsync(string origin, string? returnPath, CancellationToken cancellationToken)
    {
        if (hosts.SignInOrigin is null || origin == hosts.SignInOrigin || !await hosts.CanOpenAsync(origin, null, cancellationToken))
            return null;
        if (returnPath is { Length: > AuthenticationContinuation.MaximumReturnPathLength } ||
            !ReturnUrlPolicy.IsSafeLocal(returnPath))
            return null;
        string token = TenantHandoffTokens.Create();
        string browserCorrelation = TenantHandoffTokens.Create();
        DateTime expires = clock.UtcNow + AuthenticationContinuation.Lifetime;
        await store.AddAsync(new AuthenticationContinuation
        {
            TokenHash = TenantHandoffTokens.Hash(token),
            Origin = origin,
            ReturnPath = ReturnUrlPolicy.NormalizeOrDefault(returnPath).Value,
            CorrelationHash = TenantHandoffTokens.Hash(browserCorrelation),
            ExpiresAt = expires
        }, cancellationToken);
        correlation.Write(browserCorrelation, expires);
        await auditor.RecordAsync(AuthenticationSecurityAuditEvents.ContinuationBegin, AuthenticationSecurityAuditEvents.ContinuationBeginOperation, null, null, cancellationToken);
        return token;
    }

    public async Task<string?> CompleteAsync(string token, CancellationToken cancellationToken)
    {
        if (!TenantHandoffTokens.IsWellFormed(token) || string.IsNullOrWhiteSpace(requestContext.UserId) ||
            !await hosts.IsCentralHostAsync(cancellationToken))
            return null;
        AuthenticationContinuation? continuation = await store.FindAsync(TenantHandoffTokens.Hash(token), cancellationToken);
        if (continuation is null || continuation.CompletedAt is not null || continuation.ExpiresAt <= clock.UtcNow ||
            !await hosts.CanOpenAsync(continuation.Origin, requestContext.UserId, cancellationToken))
            return null;
        AuthenticationUserDescriptor? user = await users.FindByIdAsync(requestContext.UserId, cancellationToken);
        if (user is null) return null;
        string continuePath = AuthenticationBrowserPaths.Continue + "?" + AuthenticationBrowserQuery.Request + "=" + Uri.EscapeDataString(token);
        string next = await redirects.ResolveAsync(continuePath, user.RequiresProfileCompletion, user.UserId, cancellationToken);
        if (!string.Equals(next, continuePath, StringComparison.Ordinal))
            return next;
        if (!await store.TryCompleteAsync(continuation, clock.UtcNow, cancellationToken))
            return null;
        string? ticket = await handoffs.Handle(new IssueTenantHandoffCommand(
            requestContext.UserId, new Uri(continuation.Origin).Host, LinkIssuingSession: true,
            CorrelationHash: continuation.CorrelationHash, ReturnPath: continuation.ReturnPath), cancellationToken);
        return ticket is null ? null : continuation.Origin + AuthenticationBrowserPaths.Handoff + "#" + ticket;
    }
}
