using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vorn.Aaas.AuthenticationClients.Application.Commands;
using Vorn.Aaas.AuthenticationClients.Application.Queries;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;

namespace Vorn.Aaas.AuthenticationClients.Presentation.Api
{
    public static class AuthenticationClientsWebApplicationExtensions
    {
        /// <summary>
        /// Maps endpoints for Authentication Clients operations.
        /// Allows injection of custom endpoint filters via the optional configureFilters action.
        /// </summary>
        /// <param name="app">The WebApplication instance.</param>
        /// <param name="configureFilters">
        /// Optional delegate to apply additional filters/middleware to the endpoint group or individual endpoints.
        /// The action receives the IEndpointRouteBuilder (group) so you can call .AddEndpointFilter(...) on it.
        /// </param>
        public static void MapAuthenticationClientsEndpoints(
            this WebApplication app,
            Action<RouteGroupBuilder>? configureFilters = null)
        {
            var group = app
                .MapGroup("/api/authenticationclients")
                .WithTags("Authentication Clients")
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = "IdentityServerAccessToken"
                });

            // Allow consumer to add filters at the group‐level (applies to all endpoints in group)
            configureFilters?.Invoke(group);

            // GET /api/authenticationclients
            group.MapGet(string.Empty,
                async ([FromServices] ISender sender) =>
                {
                    var list = await sender.Send(new GetAuthenticationClients());
                    return Results.Ok(list);
                })
                .WithName("GetAuthenticationClients")
                .Produces<IEnumerable<AuthenticationClient>>(StatusCodes.Status200OK)
                .WithOpenApi(op =>
                {
                    op.Summary = "List all authentication clients";
                    op.Description = "Returns a collection of all registered authentication clients.";
                    return op;
                });

            // GET /api/authenticationclients/{id}
            group.MapGet("/{id}",
                async (string id, [FromServices] ISender sender) =>
                {
                    var result = await sender.Send(new GetAuthenticationClient(id));
                    return result is not null
                        ? Results.Ok(result)
                        : Results.NotFound();
                })
                .WithName("GetAuthenticationClient")
                .Produces<AuthenticationClient>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi(op =>
                {
                    op.Summary = "Get a single authentication client";
                    op.Description = "Returns the authentication client identified by **id**.";
                    op.Parameters[0].Description = "The unique identifier of the authentication client.";
                    return op;
                });

            // POST /api/authenticationclients
            group.MapPost(string.Empty,
                async ([FromBody] CreateAuthenticationClient command, [FromServices] ISender sender) =>
                {
                    var client = await sender.Send(command);
                    if (client is null)
                    {
                        // optionally handle error case
                        return Results.Problem("Could not create authentication client.", statusCode: StatusCodes.Status500InternalServerError);
                    }

                    return Results.Created($"/api/authenticationclients/{client.Id}", client);
                })
                .WithName("CreateAuthenticationClient")
                .Accepts<CreateAuthenticationClient>("application/json")
                .Produces<AuthenticationClient>(StatusCodes.Status201Created)
                .WithOpenApi(op =>
                {
                    op.Summary = "Create a new authentication client";
                    op.Description = "Creates a new authentication client and returns a **201 Created** response with a `Location` header pointing to the newly created resource.";
                    if (op.RequestBody is not null)
                    {
                        op.RequestBody.Description = "The command payload containing the information required to create the authentication client (e.g., Id, Secret, Scopes).";
                    }
                    return op;
                });

            // PUT /api/authenticationclients/{id}
            group.MapPut("/{id}",
                async (string id, [FromBody] UpdateAuthenticationClient command, [FromServices] ISender sender) =>
                {
                    if (id != command.Id)
                    {
                        return Results.BadRequest($"Route id '{id}' does not match payload id '{command.Id}'.");
                    }

                    await sender.Send(command);
                    return Results.NoContent();
                })
                .WithName("UpdateAuthenticationClient")
                .Accepts<UpdateAuthenticationClient>("application/json")
                .Produces<AuthenticationClient>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi(op =>
                {
                    op.Summary = "Edit an authentication client";
                    op.Description = "Updates the authentication client identified by **id**.";
                    op.Parameters[0].Description = "The unique identifier of the authentication client.";
                    if (op.RequestBody is not null)
                    {
                        op.RequestBody.Description = "The command payload containing the updated authentication client details.";
                    }
                    return op;
                });

            // DELETE /api/authenticationclients/{id}
            group.MapDelete("/{id}",
                async (string id, [FromServices] ISender sender) =>
                {
                    var command = new DeleteAuthenticationClient(id);
                    await sender.Send(command);
                    return Results.NoContent();
                })
                .WithName("DeleteAuthenticationClient")
                .Produces(StatusCodes.Status204NoContent)
                .WithOpenApi(op =>
                {
                    op.Summary = "Remove an authentication client";
                    op.Description = "Deletes the authentication client identified by **id**.";
                    op.Parameters[0].Description = "The unique identifier of the authentication client.";
                    return op;
                });
        }
    }
}
