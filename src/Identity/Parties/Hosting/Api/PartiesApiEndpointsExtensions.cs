using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Application.Queries;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Vitarish.Parties.Hosting.Api
{
    public static class PartiesApiEndpointsExtensions
    {

        public static IEndpointConventionBuilder MapOwnerEndpoints(this WebApplication app, Action<RouteGroupBuilder>? configureFilters = null)
        {
            RouteGroupBuilder group = app
                .MapGroup("/api/owners")
                .WithTags("Owners")
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = "IdentityServerAccessToken"
                });

            configureFilters?.Invoke(group);

            // GET /api/owners/{id}
            _ = group.MapGet("/{id:guid}", GetOwnerAsync)
                .WithName("GetOwner")
                .Produces<Owner>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Get an owner")
                .WithDescription("Returns the owner identified by **id**.");

            // GET /api/owners
            _ = group.MapGet(string.Empty, GetOwnersAsync)
                .WithName("GetOwners")
                .Produces<IEnumerable<Owner>>(StatusCodes.Status200OK)
                .WithSummary("List owners")
                .WithDescription("Retrieves all owners.");

            // POST /api/owners
            _ = group.MapPost(string.Empty, CreateOwnerAsync)
                .WithName("CreateOwner")
                .Accepts<CreateOwner>("application/json")
                .Produces<Owner>(StatusCodes.Status201Created)
                .WithSummary("Create an owner")
                .WithDescription("Creates a new owner from the supplied payload and returns the created resource.");

            // PUT /api/owners/{id}
            _ = group.MapPut("/{id:guid}", UpdateOwnerAsync)
                .WithName("EditOwner")
                .Accepts<UpdateOwner>("application/json")
                .Produces<Owner>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Update an owner")
                .WithDescription("Updates the owner identified by **id** using the provided payload.");

            // DELETE /api/owners/{id}
            _ = group.MapDelete("/{id:guid}", DeleteOwnerAsync)
                .WithName("RemoveOwner")
                .Produces(StatusCodes.Status204NoContent)
                .WithSummary("Delete an owner")
                .WithDescription("Deletes the owner identified by **id**.");

            return group;
        }
        /// <summary>
        /// Retrieves an owner by identifier.
        /// </summary>
        /// <param name="id">The owner identifier.</param>
        /// <param name="sender">The mediator used to dispatch the query.</param>
        /// <returns>A 200 response with the owner when found; otherwise 404.</returns>
        private static async Task<IResult> GetOwnerAsync(Guid id, [FromServices] ISender sender)
        {
            Owner? result = await sender.Send(new GetOwner(id));
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound();
        }

        /// <summary>
        /// Retrieves all owners.
        /// </summary>
        /// <param name="sender">The mediator used to dispatch the query.</param>
        /// <returns>A 200 response containing the owners.</returns>
        private static async Task<IResult> GetOwnersAsync([FromServices] ISender sender)
        {
            IEnumerable<Owner> list = await sender.Send(new GetOwners());
            return Results.Ok(list);
        }

        /// <summary>
        /// Creates a new owner from the provided command.
        /// </summary>
        /// <param name="command">The creation payload.</param>
        /// <param name="sender">The mediator used to dispatch the command.</param>
        /// <returns>A 201 response with the created owner, or a 500 problem response on failure.</returns>
        private static async Task<IResult> CreateOwnerAsync(
                [FromBody] CreateOwner command,
                [FromServices] ISender sender)
        {
            Owner? owner = await sender.Send(command);
            return owner is null
                ? Results.Problem("Could not create owner.", statusCode: StatusCodes.Status500InternalServerError)
                : Results.Created($"/api/owners/{owner.Id}", owner);
        }

        /// <summary>
        /// Updates an existing owner using the provided payload.
        /// </summary>
        /// <param name="id">The owner identifier from the route.</param>
        /// <param name="command">The update payload containing the expected identifier.</param>
        /// <param name="sender">The mediator used to dispatch the command.</param>
        /// <returns>
        /// A 200 response with the updated owner when successful, 400 when identifiers differ, or 404 when not found.
        /// </returns>
        private static async Task<IResult> UpdateOwnerAsync(
                Guid id,
                [FromBody] UpdateOwner command,
                [FromServices] ISender sender)
        {
            if (id != command.Id)
            {
                return Results.BadRequest($"Route id '{id}' does not match payload id '{command.Id}'.");
            }

            Owner? updated = await sender.Send(command);
            return updated is not null
                ? Results.Ok(updated)
                : Results.NotFound();
        }

        /// <summary>
        /// Deletes an owner by identifier.
        /// </summary>
        /// <param name="id">The owner identifier.</param>
        /// <param name="sender">The mediator used to dispatch the command.</param>
        /// <returns>A 204 response after the owner is removed.</returns>
        private static async Task<IResult> DeleteOwnerAsync(Guid id, [FromServices] ISender sender)
        {
            await sender.Send(new DeleteOwner(id));
            return Results.NoContent();
        }

        public static IEndpointConventionBuilder MapTenantEndpoints(this WebApplication app, Action<RouteGroupBuilder>? configureFilters = null)
        {
            RouteGroupBuilder group = app
                .MapGroup("/api/tenants")
                .WithTags("Tenants")
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = "IdentityServerAccessToken"
                });

            configureFilters?.Invoke(group);

            // GET /api/tenants/{id}
            _ = group.MapGet("/{id:guid}", GetTenantAsync)
                .WithName("GetTenant")
                .Produces<Tenant>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Get a tenant")
                .WithDescription("Returns the tenant identified by **id**.");

            // GET /api/tenants
            _ = group.MapGet(string.Empty, GetTenantsAsync)
                .WithName("GetTenants")
                .Produces<IEnumerable<Tenant>>(StatusCodes.Status200OK)
                .WithSummary("List tenants")
                .WithDescription("Retrieves all tenants.");

            // GET /api/owners/{ownerId}/tenants
            _ = group.MapGet("/by-owner/{ownerId:guid}", GetTenantsByOwnerAsync)
                .WithName("GetTenantsByOwner")
                .Produces<IEnumerable<Tenant>>(StatusCodes.Status200OK)
                .WithSummary("List tenants by owner")
                .WithDescription("Retrieves tenants associated with the specified **ownerId**.");

            // GET /api/tenants/by-client/{clientId}
            _ = group.MapGet("/by-client/{clientId}", GetTenantsByClientAsync)
                .WithName("GetTenantsByClientId")
                .Produces<IEnumerable<Tenant>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .WithSummary("List tenants by client")
                .WithDescription("Retrieves tenants linked to the specified **clientId**.");

            // POST /api/tenants
            _ = group.MapPost(string.Empty, CreateTenantAsync)
                .WithName("CreateTenant")
                .Accepts<CreateTenant>("application/json")
                .Produces<Tenant>(StatusCodes.Status201Created)
                .WithSummary("Create a tenant")
                .WithDescription("Creates a new tenant from the supplied payload and returns the created resource.");

            // PUT /api/tenants/{id}
            _ = group.MapPut("/{id:guid}", UpdateTenantAsync)
                .WithName("EditTenant")
                .Accepts<UpdateTenant>("application/json")
                .Produces<Tenant>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Update a tenant")
                .WithDescription("Updates the tenant identified by **id** using the provided payload.");

            // DELETE /api/tenants/{id}
            _ = group.MapDelete("/{id:guid}", DeleteTenantAsync)
                .WithName("RemoveTenant")
                .Produces(StatusCodes.Status204NoContent)
                .WithSummary("Delete a tenant")
                .WithDescription("Deletes the tenant identified by **id**.");

            return group;
        }
        /// <summary>
        /// Retrieves a tenant by identifier.
        /// </summary>
        /// <param name="id">The tenant identifier.</param>
        /// <param name="sender">The mediator used to dispatch the query.</param>
        /// <returns>A 200 response with the tenant when found; otherwise 404.</returns>
        private static async Task<IResult> GetTenantAsync(Guid id, [FromServices] ISender sender)
        {
            Tenant? result = await sender.Send(new GetTenant(id));
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound();
        }

        /// <summary>
        /// Retrieves all tenants.
        /// </summary>
        /// <param name="sender">The mediator used to dispatch the query.</param>
        /// <returns>A 200 response containing tenants.</returns>
        private static async Task<IResult> GetTenantsAsync([FromServices] ISender sender)
        {
            IEnumerable<Tenant> list = await sender.Send(new GetTenants());
            return Results.Ok(list);
        }

        /// <summary>
        /// Retrieves tenants associated with a specific owner.
        /// </summary>
        /// <param name="ownerId">The owner identifier from the route.</param>
        /// <param name="sender">The mediator used to dispatch the query.</param>
        /// <returns>A 200 response containing tenants for the owner.</returns>
        private static async Task<IResult> GetTenantsByOwnerAsync(Guid ownerId, [FromServices] ISender sender)
        {
            IEnumerable<Tenant> list = await sender.Send(new GetTenantsByOwner(ownerId));
            return Results.Ok(list);
        }

        /// <summary>
        /// Retrieves tenants linked to a specific client identifier.
        /// </summary>
        /// <param name="clientId">The client identifier from the route.</param>
        /// <param name="sender">The mediator used to dispatch the query.</param>
        /// <returns>A 200 response containing tenants for the client, or 400 when the identifier is missing.</returns>
        private static async Task<IResult> GetTenantsByClientAsync(string clientId, [FromServices] ISender sender)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Results.BadRequest("Client ID cannot be empty.");
            }

            IEnumerable<Tenant> list = await sender.Send(new GetTenantsByClientId(clientId));
            return Results.Ok(list);
        }

        /// <summary>
        /// Creates a new tenant from the provided command payload.
        /// </summary>
        /// <param name="command">The creation payload.</param>
        /// <param name="sender">The mediator used to dispatch the command.</param>
        /// <returns>A 201 response with the created tenant, or a 500 problem response on failure.</returns>
        private static async Task<IResult> CreateTenantAsync(
                [FromBody] CreateTenant command,
                [FromServices] ISender sender)
        {
            Tenant? tenant = await sender.Send(command);
            return tenant is null
                ? Results.Problem("Could not create tenant.", statusCode: StatusCodes.Status500InternalServerError)
                : Results.Created($"/api/tenants/{tenant.Id}", tenant);
        }

        /// <summary>
        /// Updates an existing tenant using the provided payload.
        /// </summary>
        /// <param name="id">The tenant identifier from the route.</param>
        /// <param name="command">The update payload containing the expected identifier.</param>
        /// <param name="sender">The mediator used to dispatch the command.</param>
        /// <returns>
        /// A 200 response with the updated tenant when successful, 400 when identifiers differ, or 404 when not found.
        /// </returns>
        private static async Task<IResult> UpdateTenantAsync(
                Guid id,
                [FromBody] UpdateTenant command,
                [FromServices] ISender sender)
        {
            if (id != command.Id)
            {
                return Results.BadRequest($"Route id '{id}' does not match payload id '{command.Id}'.");
            }

            Tenant? updated = await sender.Send(command);
            return updated is not null
                ? Results.Ok(updated)
                : Results.NotFound();
        }

        /// <summary>
        /// Deletes a tenant by identifier.
        /// </summary>
        /// <param name="id">The tenant identifier.</param>
        /// <param name="sender">The mediator used to dispatch the command.</param>
        /// <returns>A 204 response after the tenant is removed.</returns>
        private static async Task<IResult> DeleteTenantAsync(Guid id, [FromServices] ISender sender)
        {
            await sender.Send(new DeleteTenant(id));
            return Results.NoContent();
        }

        public static IEndpointConventionBuilder MapTenantIndicatorEndpoints(this WebApplication app, Action<RouteGroupBuilder>? configureFilters = null)
        {
            RouteGroupBuilder group = app
                .MapGroup("/api/tenants/{tenantId:guid}/indicators")
                .WithTags("TenantIndicators")
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = "IdentityServerAccessToken"
                });

            configureFilters?.Invoke(group);

            // GET /api/tenants/{tenantId}/indicators
            _ = group.MapGet(string.Empty, GetTenantIndicatorsAsync)
                .WithName("GetTenantIndicators")
                .Produces<IEnumerable<TenantIndicator>>(StatusCodes.Status200OK)
                .WithSummary("List tenant indicators")
                .WithDescription("Retrieves all indicators for the specified **tenantId**.");

            // POST /api/tenants/{tenantId}/indicators
            _ = group.MapPost(string.Empty, AddTenantIndicatorAsync)
                .WithName("AddTenantIndicator")
                .Produces<TenantIndicator>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Add a tenant indicator")
                .WithDescription("Creates a new indicator for **tenantId** using the supplied payload.");

            // POST /api/tenants/{tenantId}/indicators/{indicatorId}/verify
            _ = group.MapPost("/{indicatorId:guid}/verify", VerifyTenantIndicatorAsync)
                .WithName("VerifyTenantIndicator")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Verify a tenant indicator")
                .WithDescription("Marks the specified indicator as verified for **tenantId**.");

            // POST /api/tenants/{tenantId}/indicators/{indicatorId}/activate
            _ = group.MapPost("/{indicatorId:guid}/activate", ActivateTenantIndicatorAsync)
                .WithName("ActivateTenantIndicator")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Activate a tenant indicator")
                .WithDescription("Activates the specified indicator for **tenantId**.");

            // POST /api/tenants/{tenantId}/indicators/{indicatorId}/disable
            _ = group.MapPost("/{indicatorId:guid}/disable", DisableTenantIndicatorAsync)
                .WithName("DisableTenantIndicator")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Disable a tenant indicator")
                .WithDescription("Disables the specified indicator for **tenantId**.");

            // DELETE /api/tenants/{tenantId}/indicators/{indicatorId}
            _ = group.MapDelete("/{indicatorId:guid}", DeleteTenantIndicatorAsync)
                .WithName("DeleteTenantIndicator")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("Delete a tenant indicator")
                .WithDescription("Deletes the indicator identified by **indicatorId** for **tenantId**.");

            return group;
        }

        /// <summary>
        /// Retrieves indicators for the specified tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier from the route.</param>
        /// <param name="sender">The mediator used to dispatch the query.</param>
        /// <returns>A 200 response containing the tenant indicators.</returns>
        private static async Task<IResult> GetTenantIndicatorsAsync(
                Guid tenantId,
                [FromServices] ISender sender)
        {
            IEnumerable<TenantIndicator> indicators = await sender.Send(new GetTenantIndicators(tenantId));
            return Results.Ok(indicators);
        }

        /// <summary>
        /// Adds a new indicator for the specified tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier from the route.</param>
        /// <param name="command">The indicator creation payload.</param>
        /// <param name="sender">The mediator used to dispatch the command.</param>
        /// <returns>A 201 response with the created indicator, or 404 when the tenant cannot be found.</returns>
        private static async Task<IResult> AddTenantIndicatorAsync(
                Guid tenantId,
                [FromBody] AddTenantIndicator command,
                [FromServices] ISender sender)
        {
            if (tenantId != command.TenantId)
            {
                return Results.BadRequest($"Route tenantId '{tenantId}' does not match payload tenantId '{command.TenantId}'.");
            }

            TenantIndicator? indicator = await sender.Send(command);
            return indicator is not null
                ? Results.Created($"/api/tenants/{tenantId}/indicators/{indicator.Id}", indicator)
                : Results.NotFound($"Tenant '{tenantId}' not found.");
        }

        /// <summary>
        /// Verifies the specified tenant indicator.
        /// </summary>
        /// <param name="tenantId">The tenant identifier from the route.</param>
        /// <param name="indicatorId">The indicator identifier from the route.</param>
        /// <param name="sender">The mediator used to dispatch the command.</param>
        /// <returns>A 200 response when verification succeeds; otherwise 404.</returns>
        private static async Task<IResult> VerifyTenantIndicatorAsync(
                Guid tenantId,
                Guid indicatorId,
                [FromServices] ISender sender)
        {
            bool result = await sender.Send(new VerifyTenantIndicator(tenantId, indicatorId));
            return result ? Results.Ok() : Results.NotFound();
        }

        /// <summary>
        /// Activates the specified tenant indicator.
        /// </summary>
        /// <param name="tenantId">The tenant identifier from the route.</param>
        /// <param name="indicatorId">The indicator identifier from the route.</param>
        /// <param name="sender">The mediator used to dispatch the command.</param>
        /// <returns>A 200 response when activation succeeds; otherwise 404.</returns>
        private static async Task<IResult> ActivateTenantIndicatorAsync(
                Guid tenantId,
                Guid indicatorId,
                [FromServices] ISender sender)
        {
            bool result = await sender.Send(new ActivateTenantIndicator(tenantId, indicatorId));
            return result ? Results.Ok() : Results.NotFound();
        }

        /// <summary>
        /// Disables the specified tenant indicator.
        /// </summary>
        /// <param name="tenantId">The tenant identifier from the route.</param>
        /// <param name="indicatorId">The indicator identifier from the route.</param>
        /// <param name="sender">The mediator used to dispatch the command.</param>
        /// <returns>A 200 response when disable succeeds; otherwise 404.</returns>
        private static async Task<IResult> DisableTenantIndicatorAsync(
                Guid tenantId,
                Guid indicatorId,
                [FromServices] ISender sender)
        {
            bool result = await sender.Send(new DisableTenantIndicator(tenantId, indicatorId));
            return result ? Results.Ok() : Results.NotFound();
        }

        /// <summary>
        /// Deletes the specified tenant indicator.
        /// </summary>
        /// <param name="tenantId">The tenant identifier from the route.</param>
        /// <param name="indicatorId">The indicator identifier from the route.</param>
        /// <param name="sender">The mediator used to dispatch the command.</param>
        /// <returns>A 204 response when deletion succeeds; otherwise 404.</returns>
        private static async Task<IResult> DeleteTenantIndicatorAsync(
                Guid tenantId,
                Guid indicatorId,
                [FromServices] ISender sender)
        {
            bool result = await sender.Send(new DeleteTenantIndicator(tenantId, indicatorId));
            return result ? Results.NoContent() : Results.NotFound();
        }

        public static IEndpointConventionBuilder MapOnDemandTlsEndpoint(this WebApplication app) => app.MapGet("/allow", EvaluateOnDemandTlsAsync)
                .WithName("OnDemandAllow")
                .RequireRateLimiting("allow-endpoint")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status429TooManyRequests)
                .WithSummary("Allow on-demand TLS for domain")
                .WithDescription("Determines whether the provided domain is authorized for on-demand TLS based on tenant indicators.")
                .AllowAnonymous();
        /// <summary>
        /// Evaluates whether the supplied domain should be allowed for on-demand TLS issuance.
        /// </summary>
        /// <param name="req">The incoming HTTP request containing the domain query parameter.</param>
        /// <param name="db">The parties database context used to lookup tenant indicators.</param>
        /// <param name="logger">The logger used to record diagnostic information.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        /// <returns>An HTTP result indicating whether the domain is allowed.</returns>
        private static async Task<IResult> EvaluateOnDemandTlsAsync(
                HttpRequest req,
                [FromServices] PartiesDbContext db,
                [FromServices] ILogger<PartiesDbContext> logger,
                CancellationToken ct)
        {
            string host = req.Query["domain"].ToString();
            if (string.IsNullOrWhiteSpace(host))
            {
                logger.LogWarning("OnDemand TLS /allow request missing domain parameter");
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            string canonical;
            try
            {
                canonical = HostnameNormalizer.Canonical(host);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "OnDemand TLS /allow request with invalid domain: {Domain}", host);
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            bool ok = await db.TenantIndicators.AnyAsync(i =>
                (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
                (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
                i.Value == canonical, ct);

            logger.LogInformation(
                "OnDemand TLS /allow: domain={Domain}, canonical={Canonical}, decision={Decision}",
                host,
                canonical,
                ok ? "allow" : "deny");

            return ok ? Results.Ok() : Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        public static IEndpointConventionBuilder MapAdminEndpoints(this WebApplication app, Action<RouteGroupBuilder>? configureFilters = null)
        {
            RouteGroupBuilder group = app
                .MapGroup("/admin/auth-clients")
                .WithTags("Admin")
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = "IdentityServerAccessToken"
                });

            configureFilters?.Invoke(group);

            // POST /admin/auth-clients/resync
            _ = group.MapPost("/resync", ResyncAuthenticationClientsAsync)
                .WithName("ResyncAuthenticationClients")
                .Produces<ResyncAuthenticationClientsResult>(StatusCodes.Status200OK)
                .WithSummary("Resync authentication clients")
                .WithDescription("Resynchronizes authentication clients optionally scoped to **tenantId**.");

            return group;   
        }
        /// <summary>
        /// Triggers authentication client resynchronization.
        /// </summary>
        /// <param name="tenantId">Optional tenant identifier to scope the resync.</param>
        /// <param name="dryRun">Indicates whether to perform a dry-run without persisting changes.</param>
        /// <param name="sender">The mediator used to dispatch the command.</param>
        /// <returns>A 200 response containing the resync result.</returns>
        private static async Task<IResult> ResyncAuthenticationClientsAsync(
                [FromQuery] Guid? tenantId,
                [FromQuery] bool dryRun,
                [FromServices] ISender sender)
        {
            ResyncAuthenticationClientsResult result = await sender.Send(new ResyncAuthenticationClients(tenantId, dryRun));
            return Results.Ok(result);
        }


    }
}
