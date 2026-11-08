using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Domain.Sessions;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AuthenticationContinuationStore(AuthenticationDbContext database) : IAuthenticationContinuationStore
{
    public async Task AddAsync(AuthenticationContinuation continuation, CancellationToken cancellationToken)
    {
        var expired = await database.Set<AuthenticationContinuation>()
            .Where(item => item.ExpiresAt <= continuation.ExpiresAt - AuthenticationContinuation.Lifetime)
            .OrderBy(item => item.ExpiresAt).Take(100).ToListAsync(cancellationToken);
        database.RemoveRange(expired);
        database.Set<AuthenticationContinuation>().Add(continuation);
        await database.SaveChangesAsync(cancellationToken);
    }

    public Task<AuthenticationContinuation?> FindAsync(string tokenHash, CancellationToken cancellationToken) =>
        database.Set<AuthenticationContinuation>().SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

    public async Task<bool> TryCompleteAsync(AuthenticationContinuation continuation, DateTime now, CancellationToken cancellationToken)
    {
        if (continuation.CompletedAt is not null || continuation.ExpiresAt <= now)
            return false;
        continuation.CompletedAt = now;
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            database.ChangeTracker.Clear();
            return false;
        }
    }
}

public sealed class AuthenticationBrowserCorrelation(IHttpContextAccessor accessor) : IAuthenticationBrowserCorrelation
{
    private const string CookieName = "Authentication.Continuation";
    public string? Read(string correlationHash) => accessor.HttpContext?.Request.Cookies
        .FirstOrDefault(item => item.Key.StartsWith(CookieName + ".", StringComparison.Ordinal) && item.Key.EndsWith("." + correlationHash, StringComparison.Ordinal)).Value;

    public void Clear(string correlationHash)
    {
        foreach (string name in accessor.HttpContext!.Request.Cookies.Keys.Where(name =>
            name.StartsWith(CookieName + ".", StringComparison.Ordinal) && name.EndsWith("." + correlationHash, StringComparison.Ordinal)))
            accessor.HttpContext.Response.Cookies.Delete(name, new CookieOptions { Path = "/" });
    }

    public void Write(string correlation, DateTime expiresAt)
    {
        string[] existing = accessor.HttpContext!.Request.Cookies.Keys
            .Where(name => name.StartsWith(CookieName + ".", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
        foreach (string name in existing.Take(Math.Max(0, existing.Length - AuthenticationContinuation.MaximumBrowserContinuations + 1)))
            accessor.HttpContext.Response.Cookies.Delete(name, new CookieOptions { Path = "/" });
        string expiry = expiresAt.Ticks.ToString("D19", System.Globalization.CultureInfo.InvariantCulture);
        accessor.HttpContext.Response.Cookies.Append(CookieName + "." + expiry + "." + TenantHandoffTokens.Hash(correlation), correlation, new CookieOptions
        {
            HttpOnly = true, Secure = accessor.HttpContext.Request.IsHttps, SameSite = SameSiteMode.Lax, Path = "/",
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc))
        });
    }
}
