using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.ApiClients.Application.Commands;
using Vitarish.ApiClients.Application.Queries;
using Vitarish.ApiClients.Domain.Entities;

namespace Vitarish.ApiClients.Hosting.Api
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
            RouteGroupBuilder group = app
                .MapGroup("/api/apiclients")
                .WithTags("API Clients")
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = "IdentityServerAccessToken"
                });

            // Allow consumer to add filters at the group level (applies to all endpoints in group)
            configureFilters?.Invoke(group);

            // GET /api/apiclients/{id}
            _ = group.MapGet("/{id}", GetApiClientAsync)
                .WithName("GetApiClient")
                .Produces<ApiClient>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Get a single API client")
                .WithDescription("Returns the API client identified by **id**.");

            // GET /api/apiclients
            _ = group.MapGet(string.Empty, GetApiClientsAsync)
                .WithName("GetApiClients")
                .Produces<IEnumerable<ApiClient>>(StatusCodes.Status200OK)
                .WithSummary("List API clients")
                .WithDescription("Retrieves all registered API clients.");

            // POST /api/apiclients
            _ = group.MapPost(string.Empty, CreateApiClientAsync)
                .WithName("CreateApiClient")
                .Accepts<CreateApiClient>("application/json")
                .Produces<ApiClient>(StatusCodes.Status201Created)
                .WithSummary("Create an API client")
                .WithDescription("Creates a new API client from the supplied payload and returns the created resource.");

            // PUT /api/apiclients/{id}
            _ = group.MapPut("/{id}", UpdateApiClientAsync)
                .WithName("EditApiClient")
                .Accepts<UpdateApiClient>("application/json")
                .Produces<ApiClient>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Update an API client")
                .WithDescription("Updates the API client identified by **id** using the provided payload.");

            // DELETE /api/apiclients/{id}
            _ = group.MapDelete("/{id}", DeleteApiClientAsync)
                .WithName("RemoveApiClient")
                .Produces(StatusCodes.Status204NoContent)
                .WithSummary("Delete an API client")
                .WithDescription("Removes the API client identified by **id**.");

            /// <summary>
            /// Retrieves a single API client by its unique identifier.
            /// </summary>
            /// <param name="id">The identifier of the API client to fetch.</param>
            /// <param name="sender">The mediator used to dispatch the query.</param>
            /// <returns>A 200 response with the client when found; otherwise 404.</returns>
            static async Task<IResult> GetApiClientAsync(string id, [FromServices] ISender sender)
            {
                ApiClient? result = await sender.Send(new GetApiClient(id));
                return result is not null
                    ? Results.Ok(result)
                    : Results.NotFound();
            }

            /// <summary>
            /// Retrieves all API clients registered in the system.
            /// </summary>
            /// <param name="sender">The mediator used to dispatch the query.</param>
            /// <returns>A 200 response containing the collection of API clients.</returns>
            static async Task<IResult> GetApiClientsAsync([FromServices] ISender sender)
            {
                IEnumerable<ApiClient> list = await sender.Send(new GetApiClients());
                return Results.Ok(list);
            }

            /// <summary>
            /// Creates a new API client using the provided command payload.
            /// </summary>
            /// <param name="command">The details required to create the API client.</param>
            /// <param name="sender">The mediator used to dispatch the command.</param>
            /// <returns>A 201 response with the created client, or a 500 problem response on failure.</returns>
            static async Task<IResult> CreateApiClientAsync(
                [FromBody] CreateApiClient command,
                [FromServices] ISender sender)
            {
                ApiClient? client = await sender.Send(command);
                if (client is null)
                {
                    return Results.Problem(
                        "Could not create API client.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }

                return Results.Created($"/api/apiclients/{client.Id}", client);
            }

            /// <summary>
            /// Updates an existing API client with the provided values.
            /// </summary>
            /// <param name="id">The identifier of the client from the route.</param>
            /// <param name="command">The update payload, including the expected client identifier.</param>
            /// <param name="sender">The mediator used to dispatch the command.</param>
            /// <returns>
            /// A 200 response with the updated client when successful, 400 when the route and payload identifiers differ,
            /// or 404 when the client cannot be found.
            /// </returns>
            static async Task<IResult> UpdateApiClientAsync(
                string id,
                [FromBody] UpdateApiClient command,
                [FromServices] ISender sender)
            {
                if (id != command.Id)
                {
                    return Results.BadRequest($"Route id '{id}' does not match payload id '{command.Id}'.");
                }

                ApiClient? updated = await sender.Send(command);
                return updated is not null
                    ? Results.Ok(updated)
                    : Results.NotFound();
            }

            /// <summary>
            /// Deletes the specified API client.
            /// </summary>
            /// <param name="id">The identifier of the API client to delete.</param>
            /// <param name="sender">The mediator used to dispatch the command.</param>
            /// <returns>A 204 response after deletion completes.</returns>
            static async Task<IResult> DeleteApiClientAsync(string id, [FromServices] ISender sender)
            {
                await sender.Send(new DeleteApiClient(id));
                return Results.NoContent();
            }
        }
    }
}
