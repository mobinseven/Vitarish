using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Vitarish.Authentication.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure.Identity;

/// <summary>User-bearing cookies and tokens cannot outlive the account they identify.</summary>
public sealed class LiveAccountClaimsTransformation(IAuthenticationUserDirectory users) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true) return principal;
        string? subject = AuthenticationSessionClaimTypes.FindSubject(principal);
        // Client-only credentials carry no user subject.
        if (string.IsNullOrWhiteSpace(subject)) return principal;
        return await users.FindByIdAsync(subject, CancellationToken.None) is not null
            ? principal : new ClaimsPrincipal(new ClaimsIdentity());
    }
}
