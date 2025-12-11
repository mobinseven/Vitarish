using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.AuthenticationClients.Application.Commands;
using Vitarish.AuthenticationClients.Application.Queries;
using Vitarish.AuthenticationClients.Domain.Entities;

namespace Vitarish.AuthenticationClients.Hosting.Api
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
            _ = group.MapGet(string.Empty, GetAuthenticationClientsAsync)
                .WithName("GetAuthenticationClients")
                .Produces<IEnumerable<AuthenticationClient>>(StatusCodes.Status200OK)
                .WithSummary("List authentication clients")
                .WithDescription("Retrieves all authentication clients.");

            // GET /api/authenticationclients/{id}
            _ = group.MapGet("/{id}", GetAuthenticationClientAsync)
                .WithName("GetAuthenticationClient")
                .Produces<AuthenticationClient>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Get a single authentication client")
                .WithDescription("Returns the authentication client identified by **id**.");

            // POST /api/authenticationclients
            _ = group.MapPost(string.Empty, CreateAuthenticationClientAsync)
                .WithName("CreateAuthenticationClient")
                .Accepts<CreateAuthenticationClient>("application/json")
                .Produces<AuthenticationClient>(StatusCodes.Status201Created)
                .WithSummary("Create an authentication client")
                .WithDescription("Creates a new authentication client from the supplied payload and returns the created resource.");

            // PUT /api/authenticationclients/{id}
            _ = group.MapPut("/{id}", UpdateAuthenticationClientAsync)
                .WithName("UpdateAuthenticationClient")
                .Accepts<UpdateAuthenticationClient>("application/json")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Update an authentication client")
                .WithDescription(
                    "Updates the authentication client identified by **id** using the provided payload and returns no content on success.");

            // DELETE /api/authenticationclients/{id}
            _ = group.MapDelete("/{id}", DeleteAuthenticationClientAsync)
                .WithName("DeleteAuthenticationClient")
                .Produces(StatusCodes.Status204NoContent)
                .WithSummary("Delete an authentication client")
                .WithDescription("Deletes the authentication client identified by **id**.");

            /// <summary>
            /// Retrieves all authentication clients registered in the system.
            /// </summary>
            /// <param name="sender">The mediator used to dispatch the query.</param>
            /// <returns>A 200 response containing the collection of authentication clients.</returns>
            static async Task<IResult> GetAuthenticationClientsAsync([FromServices] ISender sender)
            {
                IEnumerable<AuthenticationClient> list = await sender.Send(new GetAuthenticationClients());
                return Results.Ok(list);
            }

            /// <summary>
            /// Retrieves a single authentication client by identifier.
            /// </summary>
            /// <param name="id">The authentication client identifier.</param>
            /// <param name="sender">The mediator used to dispatch the query.</param>
            /// <returns>A 200 response with the client when found; otherwise 404.</returns>
            static async Task<IResult> GetAuthenticationClientAsync(string id, [FromServices] ISender sender)
            {
                AuthenticationClient? result = await sender.Send(new GetAuthenticationClient(id));
                return result is not null
                    ? Results.Ok(result)
                    : Results.NotFound();
            }

            /// <summary>
            /// Creates a new authentication client.
            /// </summary>
            /// <param name="command">The request payload describing the new client.</param>
            /// <param name="sender">The mediator used to dispatch the command.</param>
            /// <returns>A 201 response with the created client, or a 500 problem response on failure.</returns>
            static async Task<IResult> CreateAuthenticationClientAsync(
                [FromBody] CreateAuthenticationClient command,
                [FromServices] ISender sender)
            {
                AuthenticationClient? client = await sender.Send(command);
                if (client is null)
                {
                    return Results.Problem(
                        "Could not create authentication client.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }

                return Results.Created($"/api/authenticationclients/{client.Id}", client);
            }

            /// <summary>
            /// Updates an authentication client with new details.
            /// </summary>
            /// <param name="id">The identifier from the route.</param>
            /// <param name="command">The update payload containing the identifier and new values.</param>
            /// <param name="sender">The mediator used to dispatch the command.</param>
            /// <returns>
            /// A 204 response when the update succeeds, 400 when the route and payload identifiers differ,
            /// or 404 when the client cannot be found.
            /// </returns>
            static async Task<IResult> UpdateAuthenticationClientAsync(
                string id,
                [FromBody] UpdateAuthenticationClient command,
                [FromServices] ISender sender)
            {
                if (id != command.Id)
                {
                    return Results.BadRequest($"Route id '{id}' does not match payload id '{command.Id}'.");
                }

                await sender.Send(command);
                return Results.NoContent();
            }

            /// <summary>
            /// Deletes the specified authentication client.
            /// </summary>
            /// <param name="id">The identifier of the client to delete.</param>
            /// <param name="sender">The mediator used to dispatch the command.</param>
            /// <returns>A 204 response once deletion completes.</returns>
            static async Task<IResult> DeleteAuthenticationClientAsync(string id, [FromServices] ISender sender)
            {
                DeleteAuthenticationClient command = new(id);
                await sender.Send(command);
                return Results.NoContent();
            }
        }
    }
}
