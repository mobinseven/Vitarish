using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Extensions;

/// <summary>
/// Extension methods for mapping WebAuthn credential management endpoints.
/// </summary>
public static class WebAuthnEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps WebAuthn credential management endpoints.
    /// </summary>
    public static RouteGroupBuilder MapWebAuthnManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder webauthnGroup = endpoints.MapGroup("/api/webauthn");

        // PATCH: Update WebAuthn credential friendly name
        _ = webauthnGroup.MapPatch("/credentials/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCredentialRequest request,
            UsersDbContext dbContext,
            ClaimsPrincipal user) =>
        {
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                return Results.Unauthorized();
            }

            string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            WebAuthnCredential? credential = await dbContext.WebAuthnCredentials
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (credential == null)
            {
                return Results.NotFound();
            }

            credential.FriendlyName = request.FriendlyName;
            _ = await dbContext.SaveChangesAsync();

            return Results.Ok();
        })
        .RequireAuthorization();

        return webauthnGroup;
    }
}

/// <summary>
/// Request model for updating a WebAuthn credential.
/// </summary>
public record UpdateCredentialRequest
{
    public string? FriendlyName { get; init; }
}
