using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Linq;
using Vitarish.Identity.Contracts.AuthenticationClients;
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
                IEnumerable<AuthenticationClientResponse> response = list.Select(ToResponse);
                return Results.Ok(response);
            })
            .WithName("GetAuthenticationClients")
            .Produces<IEnumerable<AuthenticationClientResponse>>(StatusCodes.Status200OK)
            .WithSummary("List all authentication clients")
            .WithDescription("Returns a collection of all registered authentication clients.");

        // GET /api/authenticationclients/{id}
        _ = group.MapGet("/{id}",
            async (string id, [FromServices] ISender sender) =>
            {
                AuthenticationClient? result = await sender.Send(new GetAuthenticationClient(id));
                return result is not null
                    ? Results.Ok(ToResponse(result))
                    : Results.NotFound();
            })
            .WithName("GetAuthenticationClient")
            .Produces<AuthenticationClientResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get a single authentication client")
            .WithDescription("Returns the authentication client identified by **id**.");

        // POST /api/authenticationclients
        _ = group.MapPost(string.Empty,
            async ([FromBody] CreateAuthenticationClientRequest request, [FromServices] ISender sender) =>
            {
                var command = new CreateAuthenticationClient(
                    request.Id,
                    request.Url,
                    request.Title,
                    request.Description,
                    request.LogoUrl,
                    request.AutoAccess);

                var result = await sender.Send(command);

                return result.IsSuccess
                    ? Results.Created($"/api/authenticationclients/{result.Value.Id}", ToResponse(result.Value))
                    : Results.Conflict(result.Error.Description);
            })
            .WithName("CreateAuthenticationClient")
            .Accepts<CreateAuthenticationClientRequest>("application/json")
            .Produces<AuthenticationClientResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict)
            .WithSummary("Create a new authentication client")
            .WithDescription("Creates a new authentication client and returns a **201 Created** response with a `Location` header pointing to the newly created resource.");

        // PUT /api/authenticationclients/{id}
        _ = group.MapPut("/{id}",
            async (string id, [FromBody] UpdateAuthenticationClientRequest request, [FromServices] ISender sender) =>
            {
                if (id != request.Id)
                {
                    return Results.BadRequest($"Route id '{id}' does not match payload id '{request.Id}'.");
                }

                var command = new UpdateAuthenticationClient(
                    request.Id,
                    request.Url,
                    request.Title,
                    request.Description,
                    request.LogoUrl,
                    request.AutoAccess);

                var result = await sender.Send(command);

                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.NotFound(result.Error.Description);
            })
            .WithName("UpdateAuthenticationClient")
            .Accepts<UpdateAuthenticationClientRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Edit an authentication client")
            .WithDescription("Updates the authentication client identified by **id**.");

        // DELETE /api/authenticationclients/{id}
        _ = group.MapDelete("/{id}",
            async (string id, [FromServices] ISender sender) =>
            {
                var result = await sender.Send(new DeleteAuthenticationClient(id));

                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.NotFound(result.Error.Description);
            })
            .WithName("DeleteAuthenticationClient")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Remove an authentication client")
            .WithDescription("Deletes the authentication client identified by **id**.");
    }

    private static AuthenticationClientResponse ToResponse(AuthenticationClient client) => new(
        client.Id,
        client.Url,
        client.Title,
        client.Description,
        client.LogoUrl,
        client.AutoAccess);
}
