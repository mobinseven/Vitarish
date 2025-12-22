using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Users.Hosting.Api
{
    public static class UserAccessApiMappingExtension
    {
        /// <summary>
        /// Maps endpoints for user-access operations.
        /// Allows injection of custom endpoint filters via the optional configureFilters action.
        /// </summary>
        /// <param name="app">The WebApplication instance.</param>
        /// <param name="configureFilters">
        /// Optional delegate to apply additional filters/middleware to the endpoint group or individual endpoints.
        /// The action receives the IEndpointRouteBuilder for the group so you can call .AddEndpointFilter(...) on it.
        /// </param>
        public static IEndpointConventionBuilder MapUserAccessEndpoints(
            this WebApplication app,
            Action<RouteGroupBuilder>? configureFilters = null)
        {
            RouteGroupBuilder group = app
                .MapGroup("/api/users/access")
                .WithTags("Users")
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = "IdentityServerAccessToken"
                });

            // Allow consumer to add group-level filters (applies to all endpoints in the group)
            configureFilters?.Invoke(group);

            _ = group.MapGet("/", GetUsers)
                .WithName("GetCurrentUser")
                .Produces(StatusCodes.Status200OK, typeof(IEnumerable<UserDto>), "application/json")
                .WithSummary("Get users by access")
                .WithDescription("Get users for current request context");

            _ = group.MapGet("/current", GetCurrentUser)
                .WithName("GetCurrentUser")
                .Produces(StatusCodes.Status200OK, typeof(UserDto), "application/json")
                .WithSummary("Get current user")
                .WithDescription("Get user for current request context");

            _ = group.MapPost("/check", CheckUserAccessAsync)
                .WithName("CheckUserAccess")
                .Accepts<UserAccess>("application/json")
                .Produces(StatusCodes.Status202Accepted)
                .Produces(StatusCodes.Status204NoContent)
                .WithSummary("Check user access")
                .WithDescription("Check if the given user access combination exists");

            return group;
        }

        private static async Task<IResult> GetUsers(
            [FromServices] IMediator mediator)
        {
            IEnumerable<User> res = await mediator.Send(new UsersByAccessQuery());
            return Results.Ok(res.Select(u => new UserDto(u)));
        }

        private static async Task<IResult> GetCurrentUser(
            [FromServices] IMediator mediator)
        {
            User? res = await mediator.Send(new GetCurrentUserQuery());
            return Results.Ok(new UserDto(res));
        }

        private static async Task<IResult> CheckUserAccessAsync(
            [FromBody] CheckUserAccessQuery command,
            [FromServices] IMediator mediator)
        {
            bool exists = await mediator.Send(command);
            return exists ? Results.Accepted() : Results.NoContent();
        }
    }
}
