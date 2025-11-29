using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vorn.Aaas.ApiClients.Application.Commands;
using Vorn.Aaas.ApiClients.Application.Queries;
using Vorn.Aaas.ApiClients.Domain.Entities;

namespace Vorn.Aaas.ApiClients.Presentation.Api
{
    public static class ApiClientsApiEndpointsExtensions
    {
        /// <summary>
        /// Maps endpoints for API Clients operations.
        /// Allows injection of custom endpoint filters via the optional configureFilters action.
        /// </summary>
        /// <param name="app">The WebApplication instance.</param>
        /// <param name="configureFilters">
        /// Optional delegate to apply additional filters/middleware to the endpoint group or individual endpoints.
        /// The action receives the IEndpointRouteBuilder for the group so you can call e.g. .AddEndpointFilter(...) on a handler.
        /// </param>
        public static void MapApiClientsEndpoints(
            this WebApplication app,
            Action<RouteGroupBuilder>? configureFilters = null)
        {
            var group = app
                .MapGroup("/api/apiclients")
                .WithTags("API Clients")
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = "IdentityServerAccessToken"
                });

            // Allow consumer to add filters at the group level (applies to all endpoints in group)
            configureFilters?.Invoke(group);

            // GET /api/apiclients/{id}
            group.MapGet("/{id}", async (string id, [FromServices] ISender sender) =>
            {
                var result = await sender.Send(new GetApiClient(id));
                return result is not null
                    ? Results.Ok(result)
                    : Results.NotFound();
            })
                .WithName("GetApiClient")
                .Produces<ApiClient>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi(op =>
                {
                    op.Summary = "Get a single API client";
                    op.Description = "Returns the API client identified by **id**.";
                    op.Parameters[0].Description = "The unique identifier of the API client.";
                    return op;
                });

            // GET /api/apiclients
            group.MapGet(string.Empty, async ([FromServices] ISender sender) =>
            {
                var list = await sender.Send(new GetApiClients());
                return Results.Ok(list);
            })
                .WithName("GetApiClients")
                .Produces<IEnumerable<ApiClient>>(StatusCodes.Status200OK)
                .WithOpenApi(op =>
                {
                    op.Summary = "List all API clients";
                    op.Description = "Returns a collection of all registered API clients.";
                    return op;
                });

            // POST /api/apiclients
            group.MapPost(string.Empty, async (
                    [FromBody] CreateApiClient command,
                    [FromServices] ISender sender) =>
            {
                var client = await sender.Send(command);
                if (client is null)
                {
                    // optionally handle error case
                    return Results.Problem("Could not create API client.", statusCode: StatusCodes.Status500InternalServerError);
                }

                return Results.Created($"/api/apiclients/{client.Id}", client);
            })
                .WithName("CreateApiClient")
                .Accepts<CreateApiClient>("application/json")
                .Produces<ApiClient>(StatusCodes.Status201Created)
                .WithOpenApi(op =>
                {
                    op.Summary = "Create a new API client";
                    op.Description = "Creates a new API client and returns a **201 Created** response with a `Location` header pointing to the newly created resource.";
                    if (op.RequestBody is not null)
                    {
                        op.RequestBody.Description = "The command payload containing the information required to create the API client (e.g., Id, Secret, Scopes).";
                    }
                    return op;
                });

            // PUT /api/apiclients/{id}
            group.MapPut("/{id}", async (
                    string id,
                    [FromBody] UpdateApiClient command,
                    [FromServices] ISender sender) =>
            {
                if (id != command.Id)
                {
                    return Results.BadRequest($"Route id '{id}' does not match payload id '{command.Id}'.");
                }

                var updated = await sender.Send(command);
                return updated is not null
                    ? Results.Ok(updated)
                    : Results.NotFound();
            })
                .WithName("EditApiClient")
                .Accepts<UpdateApiClient>("application/json")
                .Produces<ApiClient>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi(op =>
                {
                    op.Summary = "Edit an API client";
                    op.Description = "Updates the API client identified by **id**.";
                    op.Parameters[0].Description = "The unique identifier of the API client.";
                    if (op.RequestBody is not null)
                    {
                        op.RequestBody.Description = "The command payload containing the updated API client information.";
                    }
                    return op;
                });

            // DELETE /api/apiclients/{id}
            group.MapDelete("/{id}", async (
                    string id,
                    [FromServices] ISender sender) =>
            {
                await sender.Send(new DeleteApiClient(id));
                return Results.NoContent();
            })
                .WithName("RemoveApiClient")
                .Produces(StatusCodes.Status204NoContent)
                .WithOpenApi(op =>
                {
                    op.Summary = "Delete an API client";
                    op.Description = "Removes the API client identified by **id**.";
                    op.Parameters[0].Description = "The unique identifier of the API client.";
                    return op;
                });
        }
    }
}
