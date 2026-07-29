using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Modules.Base.Core.Identity;

namespace Vitarish.Clinic.Infrastructure.PatientAgent;

/// <summary>Builds the patient-agent scope from the authenticated HTTP request.</summary>
public sealed class HttpPatientAgentContextFactory(IHttpContextAccessor httpContextAccessor) : IPatientAgentContextFactory
{
    private const string AccessClaimType = "access";
    private const string TenantClaimType = "tenant";
    private const string DefaultLocale = "fa-IR";
    private const string DefaultTimeZone = "Asia/Tehran";

    public Task<PatientAgentContext> CreateAsync(
        Guid routePatientId,
        string? conversationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        HttpContext http = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("A patient-agent request requires an HTTP context.");
        ClaimsPrincipal user = http.User;
        string practiceId = user.FindFirstValue(TenantClaimType)
            ?? throw new UnauthorizedAccessException("The authenticated request has no practice scope.");
        AuthenticatedUserIdResolution userIdResolution = AuthenticatedUserIdResolver.Resolve(user);
        string authenticatedUserId = userIdResolution is AuthenticatedUserIdResolution.Success resolved
            ? resolved.UserId.Value
            : throw new UnauthorizedAccessException("The authenticated request has no user identity.");

        IReadOnlySet<string> permissions = user.FindAll(AccessClaimType)
            .Select(claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);

        return Task.FromResult(new PatientAgentContext(
            practiceId,
            routePatientId,
            authenticatedUserId,
            permissions,
            user.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            ResolveLocale(http.Request.Headers.AcceptLanguage),
            http.Request.Headers["X-Time-Zone"].FirstOrDefault() ?? DefaultTimeZone,
            string.IsNullOrWhiteSpace(conversationId) ? Guid.NewGuid().ToString("N") : conversationId.Trim()));
    }

    private static string ResolveLocale(string? acceptLanguage)
    {
        string? candidate = acceptLanguage?
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?
            .Split(';', 2)[0];
        if (string.IsNullOrWhiteSpace(candidate)) return DefaultLocale;

        try
        {
            return CultureInfo.GetCultureInfo(candidate).Name;
        }
        catch (CultureNotFoundException)
        {
            return DefaultLocale;
        }
    }
}
