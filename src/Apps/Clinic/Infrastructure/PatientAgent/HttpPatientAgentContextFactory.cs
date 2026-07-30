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
    private const string DefaultPersianCalendar = "persian";
    private const string DefaultCalendar = "gregory";
    private static readonly IReadOnlySet<string> SupportedCalendars =
        new HashSet<string>(StringComparer.Ordinal) { DefaultCalendar, DefaultPersianCalendar };

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

        string locale = ResolveLocale(http.Request.Headers.AcceptLanguage);
        bool isNewConversation = string.IsNullOrWhiteSpace(conversationId);
        return Task.FromResult(new PatientAgentContext(
            practiceId,
            routePatientId,
            authenticatedUserId,
            permissions,
            user.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            locale,
            ResolveTimeZone(http.Request.Headers["X-Time-Zone"].FirstOrDefault()),
            isNewConversation ? Guid.CreateVersion7().ToString("N") : conversationId!.Trim(),
            Calendar: ResolveCalendar(http.Request.Headers["X-Calendar"].FirstOrDefault(), locale),
            IsNewConversation: isNewConversation));
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

    private static string ResolveTimeZone(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return DefaultTimeZone;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(candidate.Trim()).Id;
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return DefaultTimeZone;
        }
    }

    private static string ResolveCalendar(string? candidate, string locale)
    {
        string? normalized = candidate?.Trim().ToLowerInvariant();
        if (normalized is not null && SupportedCalendars.Contains(normalized)) return normalized;
        return locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase)
            ? DefaultPersianCalendar
            : DefaultCalendar;
    }
}
