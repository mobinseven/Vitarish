using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vorn.Aaas.Parties.Application.Commands;
using Vorn.Aaas.Parties.Application.Queries;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Domain.Enums;
using Vorn.Aaas.Parties.Infrastructure.Data;
using Vorn.Aaas.Parties.Infrastructure.Extensions;

namespace Vorn.Aaas.Parties.Presentation.Api
{
    public static class PartiesApiEndpointsExtensions
    {
        /// <summary>
        /// Maps endpoints for Parties (Owners and Tenants) operations.
        /// Allows injection of custom endpoint filters via the optional configureFilters action.
        /// </summary>
        /// <param name="app">The WebApplication instance.</param>
        /// <param name="configureFilters">
        /// Optional delegate to apply additional filters/middleware to the endpoint group or individual endpoints.
        /// The action receives the IEndpointRouteBuilder for the group so you can call e.g. .AddEndpointFilter(...) on a handler.
        /// </param>
        public static void MapPartiesEndpoints(
            this WebApplication app,
            Action<RouteGroupBuilder>? configureFilters = null)
        {
            MapOwnerEndpoints(app, configureFilters);
            MapTenantEndpoints(app, configureFilters);
            MapTenantIndicatorEndpoints(app, configureFilters);
            MapOnDemandTlsEndpoint(app);
            MapAdminEndpoints(app, configureFilters);
        }

        private static void MapOwnerEndpoints(WebApplication app, Action<RouteGroupBuilder>? configureFilters)
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
            _ = group.MapGet("/{id:guid}", async (Guid id, [FromServices] ISender sender) =>
            {
                Owner? result = await sender.Send(new GetOwner(id));
                return result is not null
                    ? Results.Ok(result)
                    : Results.NotFound();
            })
                .WithName("GetOwner")
                .Produces<Owner>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi(op =>
                {
                    op.Summary = "Get a single owner";
                    op.Description = "Returns the owner identified by **id**.";
                    op.Parameters[0].Description = "The unique identifier of the owner.";
                    return op;
                });

            // GET /api/owners
            _ = group.MapGet(string.Empty, async ([FromServices] ISender sender) =>
            {
                IEnumerable<Owner> list = await sender.Send(new GetOwners());
                return Results.Ok(list);
            })
                .WithName("GetOwners")
                .Produces<IEnumerable<Owner>>(StatusCodes.Status200OK)
                .WithOpenApi(op =>
                {
                    op.Summary = "List all owners";
                    op.Description = "Returns a collection of all registered owners.";
                    return op;
                });

            // POST /api/owners
            _ = group.MapPost(string.Empty, async (
                    [FromBody] CreateOwner command,
                    [FromServices] ISender sender) =>
            {
                Owner? owner = await sender.Send(command);
                return owner is null
                    ? Results.Problem("Could not create owner.", statusCode: StatusCodes.Status500InternalServerError)
                    : Results.Created($"/api/owners/{owner.Id}", owner);
            })
                .WithName("CreateOwner")
                .Accepts<CreateOwner>("application/json")
                .Produces<Owner>(StatusCodes.Status201Created)
                .WithOpenApi(op =>
                {
                    op.Summary = "Create a new owner";
                    op.Description = "Creates a new owner and returns a **201 Created** response with a `Location` header pointing to the newly created resource.";
                    if (op.RequestBody is not null)
                    {
                        op.RequestBody.Description = "The command payload containing the information required to create the owner.";
                    }
                    return op;
                });

            // PUT /api/owners/{id}
            _ = group.MapPut("/{id:guid}", async (
                    Guid id,
                    [FromBody] UpdateOwner command,
                    [FromServices] ISender sender) =>
            {
                if (id != command.Id)
                {
                    return Results.BadRequest($"Route id '{id}' does not match payload id '{command.Id}'.");
                }

                Owner? updated = await sender.Send(command);
                return updated is not null
                    ? Results.Ok(updated)
                    : Results.NotFound();
            })
                .WithName("EditOwner")
                .Accepts<UpdateOwner>("application/json")
                .Produces<Owner>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi(op =>
                {
                    op.Summary = "Edit an owner";
                    op.Description = "Updates the owner identified by **id**.";
                    op.Parameters[0].Description = "The unique identifier of the owner.";
                    if (op.RequestBody is not null)
                    {
                        op.RequestBody.Description = "The command payload containing the updated owner information.";
                    }
                    return op;
                });

            // DELETE /api/owners/{id}
            _ = group.MapDelete("/{id:guid}", async (
                    Guid id,
                    [FromServices] ISender sender) =>
            {
                await sender.Send(new DeleteOwner(id));
                return Results.NoContent();
            })
                .WithName("RemoveOwner")
                .Produces(StatusCodes.Status204NoContent)
                .WithOpenApi(op =>
                {
                    op.Summary = "Delete an owner";
                    op.Description = "Removes the owner identified by **id**.";
                    op.Parameters[0].Description = "The unique identifier of the owner.";
                    return op;
                });
        }

        private static void MapTenantEndpoints(WebApplication app, Action<RouteGroupBuilder>? configureFilters)
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
            _ = group.MapGet("/{id:guid}", async (Guid id, [FromServices] ISender sender) =>
            {
                Tenant? result = await sender.Send(new GetTenant(id));
                return result is not null
                    ? Results.Ok(result)
                    : Results.NotFound();
            })
                .WithName("GetTenant")
                .Produces<Tenant>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi(op =>
                {
                    op.Summary = "Get a single tenant";
                    op.Description = "Returns the tenant identified by **id**.";
                    op.Parameters[0].Description = "The unique identifier of the tenant.";
                    return op;
                });

            // GET /api/tenants
            _ = group.MapGet(string.Empty, async ([FromServices] ISender sender) =>
            {
                IEnumerable<Tenant> list = await sender.Send(new GetTenants());
                return Results.Ok(list);
            })
                .WithName("GetTenants")
                .Produces<IEnumerable<Tenant>>(StatusCodes.Status200OK)
                .WithOpenApi(op =>
                {
                    op.Summary = "List all tenants";
                    op.Description = "Returns a collection of all registered tenants.";
                    return op;
                });

            // GET /api/owners/{ownerId}/tenants
            _ = group.MapGet("/by-owner/{ownerId:guid}", async (Guid ownerId, [FromServices] ISender sender) =>
            {
                IEnumerable<Tenant> list = await sender.Send(new GetTenantsByOwner(ownerId));
                return Results.Ok(list);
            })
                .WithName("GetTenantsByOwner")
                .Produces<IEnumerable<Tenant>>(StatusCodes.Status200OK)
                .WithOpenApi(op =>
                {
                    op.Summary = "List tenants by owner";
                    op.Description = "Returns a collection of tenants managed by the specified owner.";
                    op.Parameters[0].Description = "The unique identifier of the owner.";
                    return op;
                });

            // GET /api/tenants/by-client/{clientId}
            _ = group.MapGet("/by-client/{clientId}", async (string clientId, [FromServices] ISender sender) =>
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    return Results.BadRequest("Client ID cannot be empty.");
                }
                IEnumerable<Tenant> list = await sender.Send(new GetTenantsByClientId(clientId));
                return Results.Ok(list);
            })
                .WithName("GetTenantsByClientId")
                .Produces<IEnumerable<Tenant>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .WithOpenApi(op =>
                {
                    op.Summary = "List tenants by client ID";
                    op.Description = "Returns a collection of tenants associated with the specified client. Returns an empty array if the client has no tenants.";
                    op.Parameters[0].Description = "The client identifier.";
                    return op;
                });

            // POST /api/tenants
            _ = group.MapPost(string.Empty, async (
                    [FromBody] CreateTenant command,
                    [FromServices] ISender sender) =>
            {
                Tenant? tenant = await sender.Send(command);
                return tenant is null
                    ? Results.Problem("Could not create tenant.", statusCode: StatusCodes.Status500InternalServerError)
                    : Results.Created($"/api/tenants/{tenant.Id}", tenant);
            })
                .WithName("CreateTenant")
                .Accepts<CreateTenant>("application/json")
                .Produces<Tenant>(StatusCodes.Status201Created)
                .WithOpenApi(op =>
                {
                    op.Summary = "Create a new tenant";
                    op.Description = "Creates a new tenant and returns a **201 Created** response with a `Location` header pointing to the newly created resource.";
                    if (op.RequestBody is not null)
                    {
                        op.RequestBody.Description = "The command payload containing the information required to create the tenant.";
                    }
                    return op;
                });

            // PUT /api/tenants/{id}
            _ = group.MapPut("/{id:guid}", async (
                    Guid id,
                    [FromBody] UpdateTenant command,
                    [FromServices] ISender sender) =>
            {
                if (id != command.Id)
                {
                    return Results.BadRequest($"Route id '{id}' does not match payload id '{command.Id}'.");
                }

                Tenant? updated = await sender.Send(command);
                return updated is not null
                    ? Results.Ok(updated)
                    : Results.NotFound();
            })
                .WithName("EditTenant")
                .Accepts<UpdateTenant>("application/json")
                .Produces<Tenant>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi(op =>
                {
                    op.Summary = "Edit a tenant";
                    op.Description = "Updates the tenant identified by **id**.";
                    op.Parameters[0].Description = "The unique identifier of the tenant.";
                    if (op.RequestBody is not null)
                    {
                        op.RequestBody.Description = "The command payload containing the updated tenant information.";
                    }
                    return op;
                });

            // DELETE /api/tenants/{id}
            _ = group.MapDelete("/{id:guid}", async (
                    Guid id,
                    [FromServices] ISender sender) =>
            {
                await sender.Send(new DeleteTenant(id));
                return Results.NoContent();
            })
                .WithName("RemoveTenant")
                .Produces(StatusCodes.Status204NoContent)
                .WithOpenApi(op =>
                {
                    op.Summary = "Delete a tenant";
                    op.Description = "Removes the tenant identified by **id**.";
                    op.Parameters[0].Description = "The unique identifier of the tenant.";
                    return op;
                });
        }

        private static void MapTenantIndicatorEndpoints(WebApplication app, Action<RouteGroupBuilder>? configureFilters)
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
            _ = group.MapGet(string.Empty, async (
                    Guid tenantId,
                    [FromServices] ISender sender) =>
            {
                IEnumerable<TenantIndicator> indicators = await sender.Send(new GetTenantIndicators(tenantId));
                return Results.Ok(indicators);
            })
                .WithName("GetTenantIndicators")
                .Produces<IEnumerable<TenantIndicator>>(StatusCodes.Status200OK)
                .WithOpenApi();

            // POST /api/tenants/{tenantId}/indicators
            _ = group.MapPost(string.Empty, async (
                    Guid tenantId,
                    [FromBody] AddTenantIndicator command,
                    [FromServices] ISender sender) =>
            {
                if (tenantId != command.TenantId)
                {
                    return Results.BadRequest($"Route tenantId '{tenantId}' does not match payload tenantId '{command.TenantId}'.");
                }

                TenantIndicator? indicator = await sender.Send(command);
                return indicator is not null
                    ? Results.Created($"/api/tenants/{tenantId}/indicators/{indicator.Id}", indicator)
                    : Results.NotFound($"Tenant '{tenantId}' not found.");
            })
                .WithName("AddTenantIndicator")
                .Produces<TenantIndicator>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi();

            // POST /api/tenants/{tenantId}/indicators/{indicatorId}/verify
            _ = group.MapPost("/{indicatorId:guid}/verify", async (
                    Guid tenantId,
                    Guid indicatorId,
                    [FromServices] ISender sender) =>
            {
                bool result = await sender.Send(new VerifyTenantIndicator(tenantId, indicatorId));
                return result ? Results.Ok() : Results.NotFound();
            })
                .WithName("VerifyTenantIndicator")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi();

            // POST /api/tenants/{tenantId}/indicators/{indicatorId}/activate
            _ = group.MapPost("/{indicatorId:guid}/activate", async (
                    Guid tenantId,
                    Guid indicatorId,
                    [FromServices] ISender sender) =>
            {
                bool result = await sender.Send(new ActivateTenantIndicator(tenantId, indicatorId));
                return result ? Results.Ok() : Results.NotFound();
            })
                .WithName("ActivateTenantIndicator")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi();

            // POST /api/tenants/{tenantId}/indicators/{indicatorId}/disable
            _ = group.MapPost("/{indicatorId:guid}/disable", async (
                    Guid tenantId,
                    Guid indicatorId,
                    [FromServices] ISender sender) =>
            {
                bool result = await sender.Send(new DisableTenantIndicator(tenantId, indicatorId));
                return result ? Results.Ok() : Results.NotFound();
            })
                .WithName("DisableTenantIndicator")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi();

            // DELETE /api/tenants/{tenantId}/indicators/{indicatorId}
            _ = group.MapDelete("/{indicatorId:guid}", async (
                    Guid tenantId,
                    Guid indicatorId,
                    [FromServices] ISender sender) =>
            {
                bool result = await sender.Send(new DeleteTenantIndicator(tenantId, indicatorId));
                return result ? Results.NoContent() : Results.NotFound();
            })
                .WithName("DeleteTenantIndicator")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound)
                .WithOpenApi();
        }

        private static void MapOnDemandTlsEndpoint(WebApplication app) => app.MapGet("/allow", async (
                                                                                           HttpRequest req,
                                                                                           [FromServices] PartiesDbContext db,
                                                                                           [FromServices] ILogger<PartiesDbContext> logger,
                                                                                           CancellationToken ct) =>
                                                                                   {
                                                                                       string host = req.Query["domain"].ToString();
                                                                                       if (string.IsNullOrWhiteSpace(host))
                                                                                       {
                                                                                           logger.LogWarning("OnDemand TLS /allow request missing domain parameter");
                                                                                           return Results.StatusCode(400);
                                                                                       }

                                                                                       string canonical;
                                                                                       try
                                                                                       {
                                                                                           canonical = HostnameNormalizer.Canonical(host);
                                                                                       }
                                                                                       catch (ArgumentException ex)
                                                                                       {
                                                                                           logger.LogWarning(ex, "OnDemand TLS /allow request with invalid domain: {Domain}", host);
                                                                                           return Results.StatusCode(400);
                                                                                       }

                                                                                       bool ok = await db.TenantIndicators.AnyAsync(i =>
                                                                                           (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
                                                                                           (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
                                                                                           i.Value == canonical, ct);

                                                                                       logger.LogInformation("OnDemand TLS /allow: domain={Domain}, canonical={Canonical}, decision={Decision}",
                                                                                           host, canonical, ok ? "allow" : "deny");

                                                                                       return ok ? Results.Ok() : Results.StatusCode(403);
                                                                                   })
                .WithName("OnDemandAllow")
                .RequireRateLimiting("allow-endpoint")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status429TooManyRequests)
                .AllowAnonymous()
                .WithOpenApi(op =>
                {
                    op.Summary = "On-demand TLS certificate allowance check";
                    op.Description = "Returns 2xx if the domain is allowed for certificate issuance, 403 otherwise. Rate limited to 50 requests per minute per IP address.";
                    return op;
                });

        private static void MapAdminEndpoints(WebApplication app, Action<RouteGroupBuilder>? configureFilters)
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
            _ = group.MapPost("/resync", async (
                    [FromQuery] Guid? tenantId,
                    [FromQuery] bool dryRun,
                    [FromServices] ISender sender) =>
            {
                ResyncAuthenticationClientsResult result = await sender.Send(new ResyncAuthenticationClients(tenantId, dryRun));
                return Results.Ok(result);
            })
                .WithName("ResyncAuthenticationClients")
                .Produces<ResyncAuthenticationClientsResult>(StatusCodes.Status200OK)
                .WithOpenApi(op =>
                {
                    op.Summary = "Resync authentication clients from tenant indicators";
                    op.Description = "Re-projects all indicators for a tenant (or all tenants if omitted) to authentication clients. Use dryRun=true to preview changes.";
                    return op;
                });
        }


    }
}
