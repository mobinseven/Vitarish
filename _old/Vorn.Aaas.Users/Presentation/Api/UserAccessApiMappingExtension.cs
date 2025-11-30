using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vorn.Aaas.Users.Application.Commands;
using Vorn.Aaas.Users.Application.Queries;
using Vorn.Aaas.Users.Presentation.Contracts;

namespace Vorn.Aaas.Users.Presentation.Api
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
            _ = group.MapGet("/clients/{clientId}/{userId}",
                async (string clientId, string userId, [FromServices] IMediator mediator) =>
                {
                    UserDto? result = await mediator.Send(new GetClientUserQuery(clientId, userId));
                    return result is not null
                        ? Results.Ok(result)
                        : Results.NotFound();
                })
                .WithName("GetClientUser")
                .Produces<UserDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi(op =>
                {
                    op.Summary = "Get a user with access to a client";
                    op.Description = "Returns the user that holds an access claim for the specified client.";
                    op.Parameters[0].Description = "The unique identifier of the client / application.";   // clientId
                    op.Parameters[1].Description = "The unique identifier of the user."; // userId
                    return op;
                });

            // GET /api/users/{userId}/claims
            _ = group.MapGet("/{userId}/claims",
                async (string userId, [FromServices] IMediator mediator) =>
                {
                    IEnumerable<ClaimDto> claims = await mediator.Send(new GetUserClaimsQuery(userId));
                    return Results.Ok(claims);
                })
                .WithName("GetUserClaims")
                .Produces<IEnumerable<ClaimDto>>(StatusCodes.Status200OK)
                .WithOpenApi(op =>
                {
                    op.Summary = "Get all claims for a user";
                    op.Description = "Returns every claim associated with the specified user identifier.";
                    op.Parameters[0].Description = "The unique identifier of the user."; // userId
                    return op;
                });

            // GET /api/users/{userId}/access/{clientId}
            _ = group.MapGet("/{userId}/access/{clientId}",
                async (string userId, string clientId, [FromServices] IMediator mediator) =>
                {
                    ClaimDto? claim = await mediator.Send(new GetUserAccessClaimQuery(userId, clientId));
                    return claim is not null
                        ? Results.Ok(claim)
                        : Results.NotFound();
                })
                .WithName("GetUserAccess")
                .Produces<ClaimDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi(op =>
                {
                    op.Summary = "Get a user's access claim for a client";
                    op.Description = "Returns the specific access claim (if any) that the user holds for the given client identifier.";
                    op.Parameters[0].Description = "The unique identifier of the user.";   // userId
                    op.Parameters[1].Description = "The unique identifier of the client / application."; // clientId
                    return op;
                });

            _ = group.MapGet("/{userId}/hasaccess/{clientId}",
                async (string userId, string clientId, [FromServices] IMediator mediator) =>
                    Results.Ok(await mediator.Send(new HasUserAccessQuery(userId, clientId))))
                 .WithName("HasUserAccess")
                 .Produces<bool>(StatusCodes.Status200OK)
                 .WithOpenApi(op =>
                 {
                     op.Summary = "Check whether a user has access to a client";
                     op.Description = "Returns **true** if the user possesses an access claim for the specified client; otherwise **false**.";
                     op.Parameters[0].Description = "The unique identifier of the user."; // userId
                     op.Parameters[1].Description = "The unique identifier of the client / application."; // clientId
                     return op;
                 });

            // GET /api/users/clients/{clientId}
            _ = group.MapGet("/clients/{clientId}",
                async (string clientId, [FromServices] IMediator mediator) =>
                {
                    IEnumerable<UserDto> users = await mediator.Send(new GetClientUsersQuery(clientId));
                    return Results.Ok(users);
                })
                .WithName("GetClientUsers")
                .Produces<IEnumerable<UserDto>>(StatusCodes.Status200OK)
                .WithOpenApi(op =>
                {
                    op.Summary = "List all users with access to a client";
                    op.Description = "Returns a collection of users that currently hold an access claim for the specified client.";
                    op.Parameters[0].Description = "The unique identifier of the client / application."; // clientId
                    return op;
                });

            // POST /api/users/access
            _ = group.MapPost("/access",
                async ([FromBody] SetUserAccessClaimCommand command, [FromServices] IMediator mediator) =>
                {
                    await mediator.Send(command);
                    return Results.Ok();
                })
                .WithName("SetUserAccess")
                .Accepts<SetUserAccessClaimCommand>("application/json")
                .Produces(StatusCodes.Status200OK)
                .WithOpenApi(op =>
                {
                    op.Summary = "Grant or update a user's access claim";
                    op.Description = "Creates or updates an access claim for the specified user and client. The full command payload must be sent in the request body.";
                    if (op.RequestBody is not null)
                    {
                        op.RequestBody.Description = "The command payload containing userId, clientId, and the desired claim value.";
                    }

                    return op;
                });

            // DELETE /api/users/access
            _ = group.MapDelete("/access",
                async ([FromBody] RemoveUserAccessClaimCommand command, [FromServices] IMediator mediator) =>
                {
                    await mediator.Send(command);
                    return Results.NoContent();
                })
                .WithName("RemoveUserAccess")
                .Accepts<RemoveUserAccessClaimCommand>("application/json")
                .Produces(StatusCodes.Status204NoContent)
                .WithOpenApi(op =>
                {
                    op.Summary = "Remove a user's access claim";
                    op.Description = "Deletes the access claim for the given user and client. The command payload must be provided in the request body.";
                    if (op.RequestBody is not null)
                    {
                        op.RequestBody.Description = "The command payload containing userId and clientId to remove the claim for.";
                    }

                    return op;
                });
        }
    }
}
