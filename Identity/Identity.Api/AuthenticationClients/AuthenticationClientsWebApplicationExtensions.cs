using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.Identity.Core.AuthenticationClients.Application.Commands;
using Vitarish.Identity.Core.AuthenticationClients.Application.Queries;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;

namespace Vitarish.Identity.Api.AuthenticationClients;

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
        RouteGroupBuilder group = app
            .MapGroup("/api/authenticationclients")
            .WithTags("Authentication Clients")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = "IdentityServerAccessToken"
            });

        // Allow consumer to add filters at the group‐level (applies to all endpoints in group)
        configureFilters?.Invoke(group);

        // GET /api/authenticationclients
        _ = group.MapGet(string.Empty,
            async ([FromServices] ISender sender) =>
            {
                IEnumerable<AuthenticationClient> list = await sender.Send(new GetAuthenticationClients());
                return Results.Ok(list);
            })
            .WithName("GetAuthenticationClients")
            .Produces<IEnumerable<AuthenticationClient>>(StatusCodes.Status200OK)
            .WithSummary("List all authentication clients")
            .WithDescription("Returns a collection of all registered authentication clients.");

        // GET /api/authenticationclients/{id}
        _ = group.MapGet("/{id}",
            async (string id, [FromServices] ISender sender) =>
            {
                AuthenticationClient? result = await sender.Send(new GetAuthenticationClient(id));
                return result is not null
                    ? Results.Ok(result)
                    : Results.NotFound();
            })
            .WithName("GetAuthenticationClient")
            .Produces<AuthenticationClient>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get a single authentication client")
            .WithDescription("Returns the authentication client identified by **id**.");

        // POST /api/authenticationclients
        _ = group.MapPost(string.Empty,
            async ([FromBody] CreateAuthenticationClient command, [FromServices] ISender sender) =>
            {
                AuthenticationClient? client = await sender.Send(command);
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
            .WithSummary("Create a new authentication client")
            .WithDescription("Creates a new authentication client and returns a **201 Created** response with a `Location` header pointing to the newly created resource.");

        // PUT /api/authenticationclients/{id}
        _ = group.MapPut("/{id}",
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
            .WithSummary("Edit an authentication client")
            .WithDescription("Updates the authentication client identified by **id**.");

        // DELETE /api/authenticationclients/{id}
        _ = group.MapDelete("/{id}",
            async (string id, [FromServices] ISender sender) =>
            {
                DeleteAuthenticationClient command = new(id);
                await sender.Send(command);
                return Results.NoContent();
            })
            .WithName("DeleteAuthenticationClient")
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Remove an authentication client")
            .WithDescription("Deletes the authentication client identified by **id**.");
    }
}
