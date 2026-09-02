using Vitarish.Requests.Contracts;

namespace Vitarish.Authentication.Application.Abstractions;

public static class AuthenticationSessionClaimTypes
{
    public const string Subject = RequestContextClaimTypes.UserId;
    public const string SessionId = "vitarish_session_id";

    public static string? FindSubject(System.Security.Claims.ClaimsPrincipal? principal) =>
        principal?.FindFirst(Subject)?.Value
        ?? principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
}
