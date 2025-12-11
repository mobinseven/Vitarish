using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
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
        public static void MapUserAccessEndpoints(
            this WebApplication app,
            Action<RouteGroupBuilder>? configureFilters = null)
        {
            RouteGroupBuilder group = app
                .MapGroup("/api/users")
                .WithTags("Users")
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = "IdentityServerAccessToken"
                });

            // Allow consumer to add group-level filters (applies to all endpoints in the group)
            configureFilters?.Invoke(group);

            // GET /api/users/clients/{clientId}/{userId}
            _ = group.MapGet("/clients/{clientId}/{userId}", GetClientUserAsync)
                .WithName("GetClientUser")
                .Produces<UserDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Get a client user")
                .WithDescription("Returns the user identified by **userId** for the specified **clientId**.");

            // GET /api/users/{userId}/claims
            _ = group.MapGet("/{userId}/claims", GetUserClaimsAsync)
                .WithName("GetUserClaims")
                .Produces<IEnumerable<ClaimDto>>(StatusCodes.Status200OK)
                .WithSummary("List user claims")
                .WithDescription("Retrieves all claims for the user identified by **userId**.");

            // GET /api/users/{userId}/access/{clientId}
            _ = group.MapGet("/{userId}/access/{clientId}", GetUserAccessAsync)
                .WithName("GetUserAccess")
                .Produces<ClaimDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Get user access claim")
                .WithDescription("Returns the access claim for **userId** in the specified **clientId** context.");

            _ = group.MapGet("/{userId}/hasaccess/{clientId}", HasUserAccessAsync)
                .WithName("HasUserAccess")
                .Produces<bool>(StatusCodes.Status200OK)
                .WithSummary("Check user access")
                .WithDescription("Indicates whether **userId** has access within **clientId**.");

            // GET /api/users/clients/{clientId}
            _ = group.MapGet("/clients/{clientId}", GetClientUsersAsync)
                .WithName("GetClientUsers")
                .Produces<IEnumerable<UserDto>>(StatusCodes.Status200OK)
                .WithSummary("List client users")
                .WithDescription("Retrieves all users associated with **clientId**.");

            // POST /api/users/access
            _ = group.MapPost("/access", SetUserAccessAsync)
                .WithName("SetUserAccess")
                .Accepts<SetUserAccessClaimCommand>("application/json")
                .Produces(StatusCodes.Status200OK)
                .WithSummary("Grant user access")
                .WithDescription("Grants access for the provided user and client combination.");

            // DELETE /api/users/access
            _ = group.MapDelete("/access", RemoveUserAccessAsync)
                .WithName("RemoveUserAccess")
                .Accepts<RemoveUserAccessClaimCommand>("application/json")
                .Produces(StatusCodes.Status204NoContent)
                .WithSummary("Revoke user access")
                .WithDescription("Removes access for the provided user and client combination.");

            /// <summary>
            /// Retrieves a specific client user by identifiers.
            /// </summary>
            /// <param name="clientId">The client identifier from the route.</param>
            /// <param name="userId">The user identifier from the route.</param>
            /// <param name="mediator">The mediator used to dispatch the query.</param>
            /// <returns>A 200 response with the user when found; otherwise 404.</returns>
            static async Task<IResult> GetClientUserAsync(string clientId, string userId, [FromServices] IMediator mediator)
            {
                UserDto? result = await mediator.Send(new GetClientUserQuery(clientId, userId));
                return result is not null
                    ? Results.Ok(result)
                    : Results.NotFound();
            }

            /// <summary>
            /// Retrieves claims for the specified user.
            /// </summary>
            /// <param name="userId">The user identifier from the route.</param>
            /// <param name="mediator">The mediator used to dispatch the query.</param>
            /// <returns>A 200 response containing the user's claims.</returns>
            static async Task<IResult> GetUserClaimsAsync(string userId, [FromServices] IMediator mediator)
            {
                IEnumerable<ClaimDto> claims = await mediator.Send(new GetUserClaimsQuery(userId));
                return Results.Ok(claims);
            }

            /// <summary>
            /// Retrieves an access claim for the specified user and client combination.
            /// </summary>
            /// <param name="userId">The user identifier from the route.</param>
            /// <param name="clientId">The client identifier from the route.</param>
            /// <param name="mediator">The mediator used to dispatch the query.</param>
            /// <returns>A 200 response with the claim when found; otherwise 404.</returns>
            static async Task<IResult> GetUserAccessAsync(string userId, string clientId, [FromServices] IMediator mediator)
            {
                ClaimDto? claim = await mediator.Send(new GetUserAccessClaimQuery(userId, clientId));
                return claim is not null
                    ? Results.Ok(claim)
                    : Results.NotFound();
            }

            /// <summary>
            /// Indicates whether the specified user has access to the given client.
            /// </summary>
            /// <param name="userId">The user identifier.</param>
            /// <param name="clientId">The client identifier.</param>
            /// <param name="mediator">The mediator used to dispatch the query.</param>
            /// <returns>A 200 response containing a boolean that represents the access decision.</returns>
            static async Task<IResult> HasUserAccessAsync(string userId, string clientId, [FromServices] IMediator mediator)
            {
                bool hasAccess = await mediator.Send(new HasUserAccessQuery(userId, clientId));
                return Results.Ok(hasAccess);
            }

            /// <summary>
            /// Retrieves all users associated with the specified client.
            /// </summary>
            /// <param name="clientId">The client identifier.</param>
            /// <param name="mediator">The mediator used to dispatch the query.</param>
            /// <returns>A 200 response containing the users linked to the client.</returns>
            static async Task<IResult> GetClientUsersAsync(string clientId, [FromServices] IMediator mediator)
            {
                IEnumerable<UserDto> users = await mediator.Send(new GetClientUsersQuery(clientId));
                return Results.Ok(users);
            }

            /// <summary>
            /// Grants access to a user for a specific client.
            /// </summary>
            /// <param name="command">The command payload describing the access to grant.</param>
            /// <param name="mediator">The mediator used to dispatch the command.</param>
            /// <returns>A 200 response once the grant is applied.</returns>
            static async Task<IResult> SetUserAccessAsync(
                [FromBody] SetUserAccessClaimCommand command,
                [FromServices] IMediator mediator)
            {
                await mediator.Send(command);
                return Results.Ok();
            }

            /// <summary>
            /// Revokes access for a user from a specific client.
            /// </summary>
            /// <param name="command">The command payload describing the access to revoke.</param>
            /// <param name="mediator">The mediator used to dispatch the command.</param>
            /// <returns>A 204 response when the revocation succeeds.</returns>
            static async Task<IResult> RemoveUserAccessAsync(
                [FromBody] RemoveUserAccessClaimCommand command,
                [FromServices] IMediator mediator)
            {
                await mediator.Send(command);
                return Results.NoContent();
            }
        }
    }
}
