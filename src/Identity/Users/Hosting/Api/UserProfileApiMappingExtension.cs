using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Infrastructure;

namespace Vitarish.Users.Hosting.Api;

public static class UserProfileApiMappingExtension
{
    public static void MapUserProfileEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/user/profile")
            .WithTags("User Profile")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = "IdentityServerAccessToken"
            });

        _ = group.MapGet("/{userId}", GetProfileAsync)
            .WithName("GetUserProfile")
            .Produces<Dictionary<string, string>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get user profile preferences")
            .WithDescription("Returns the profile preferences for the specified user.");

        _ = group.MapPut("/{userId}", UpdateProfileAsync)
            .WithName("UpdateUserProfile")
            .Accepts<Dictionary<string, string>>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Update user profile preferences")
            .WithDescription("Replaces the profile preferences for the specified user.");

        static async Task<IResult> GetProfileAsync(
            string userId,
            ClaimsPrincipal user,
            [FromServices] IUserProfileService profileService,
            CancellationToken ct)
        {
            if (!IsAuthorizedUser(user, userId))
            {
                return Results.Forbid();
            }

            try
            {
                Dictionary<string, string> profile = await profileService.GetProfileAsync(userId, ct);
                return Results.Ok(profile);
            }
            catch (InvalidDataException)
            {
                return Results.NotFound();
            }
        }

        static async Task<IResult> UpdateProfileAsync(
            string userId,
            [FromBody] Dictionary<string, string> preferences,
            ClaimsPrincipal user,
            [FromServices] IUserProfileService profileService,
            CancellationToken ct)
        {
            if (!IsAuthorizedUser(user, userId))
            {
                return Results.Forbid();
            }

            try
            {
                await profileService.UpdateProfileAsync(userId, preferences, ct);
                return Results.NoContent();
            }
            catch (InvalidDataException)
            {
                return Results.NotFound();
            }
        }

        static bool IsAuthorizedUser(ClaimsPrincipal user, string userId)
        {
            if (user.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            string? subjectId = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(subjectId) && string.Equals(subjectId, userId, StringComparison.Ordinal))
            {
                return true;
            }

            return user.Claims.Any(static c =>
            {
                if (!string.Equals(c.Type, AaasConstants.AccessClaim, StringComparison.Ordinal))
                {
                    return false;
                }

                string[] parts = c.Value.Split(':');
                return parts.Length > 0 &&
                    string.Equals(parts[^1], AaasConstants.Admin, StringComparison.OrdinalIgnoreCase);
            });
        }
    }
}
