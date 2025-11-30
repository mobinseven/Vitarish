using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Identity.Core.Domain.Entities;
using Vitarish.Identity.Core.Domain.Enums;
using Vitarish.Identity.Core.Extensions;
using Vitarish.Identity.Contracts.Requests;
using Vitarish.Identity.Infrastructure.Data;

namespace Vitarish.Identity.Api.Endpoints;

/// <summary>
/// Extension methods for mapping Identity API endpoints.
/// </summary>
public static class IdentityApiEndpoints
{
    /// <summary>
    /// Maps all Identity API endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <param name="configureFilters">Optional action to configure filters on endpoint groups.</param>
    public static void MapIdentityEndpoints(
        this IEndpointRouteBuilder app,
        Action<RouteGroupBuilder>? configureFilters = null)
    {
        MapOwnerEndpoints(app, configureFilters);
        MapTenantEndpoints(app, configureFilters);
        MapOnDemandTlsEndpoint(app);
    }

    private static void MapOwnerEndpoints(IEndpointRouteBuilder app, Action<RouteGroupBuilder>? configureFilters)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/owners")
            .WithTags("Owners");

        configureFilters?.Invoke(group);

        // GET /api/owners
        _ = group.MapGet(string.Empty, async (IdentityDbContext db, CancellationToken ct) =>
        {
            var owners = await db.Owners.AsNoTracking().ToListAsync(ct);
            return Results.Ok(owners);
        })
            .WithName("GetOwners")
            .Produces<IEnumerable<Owner>>(StatusCodes.Status200OK)
            .WithSummary("List all owners")
            .WithDescription("Returns a collection of all registered owners.");

        // GET /api/owners/{id}
        _ = group.MapGet("/{id:guid}", async (Guid id, IdentityDbContext db, CancellationToken ct) =>
        {
            var owner = await db.Owners.FindAsync([id], ct);
            return owner is not null
                ? Results.Ok(owner)
                : Results.NotFound();
        })
            .WithName("GetOwner")
            .Produces<Owner>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get a single owner")
            .WithDescription("Returns the owner identified by id.");

        // POST /api/owners
        _ = group.MapPost(string.Empty, async (CreateOwnerRequest request, IdentityDbContext db, CancellationToken ct) =>
        {
            var owner = new Owner
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                LogoUrl = request.LogoUrl,
                Description = request.Description,
                CreatedBy = request.CreatedBy
            };
            db.Owners.Add(owner);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/owners/{owner.Id}", owner);
        })
            .WithName("CreateOwner")
            .Accepts<CreateOwnerRequest>("application/json")
            .Produces<Owner>(StatusCodes.Status201Created)
            .WithSummary("Create a new owner")
            .WithDescription("Creates a new owner and returns a 201 Created response.");

        // PUT /api/owners/{id}
        _ = group.MapPut("/{id:guid}", async (Guid id, UpdateOwnerRequest request, IdentityDbContext db, CancellationToken ct) =>
        {
            if (id != request.Id)
            {
                return Results.BadRequest($"Route id '{id}' does not match payload id '{request.Id}'.");
            }

            var owner = await db.Owners.FindAsync([id], ct);
            if (owner is null)
            {
                return Results.NotFound();
            }

            owner.Name = request.Name;
            owner.LogoUrl = request.LogoUrl;
            owner.Description = request.Description;
            owner.ModifiedBy = request.ModifiedBy;
            owner.ModifiedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return Results.Ok(owner);
        })
            .WithName("UpdateOwner")
            .Accepts<UpdateOwnerRequest>("application/json")
            .Produces<Owner>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Update an owner")
            .WithDescription("Updates the owner identified by id.");

        // DELETE /api/owners/{id}
        _ = group.MapDelete("/{id:guid}", async (Guid id, IdentityDbContext db, CancellationToken ct) =>
        {
            var owner = await db.Owners.FindAsync([id], ct);
            if (owner is null)
            {
                return Results.NotFound();
            }

            db.Owners.Remove(owner);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
            .WithName("DeleteOwner")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Delete an owner")
            .WithDescription("Removes the owner identified by id.");
    }

    private static void MapTenantEndpoints(IEndpointRouteBuilder app, Action<RouteGroupBuilder>? configureFilters)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/tenants")
            .WithTags("Tenants");

        configureFilters?.Invoke(group);

        // GET /api/tenants
        _ = group.MapGet(string.Empty, async (IdentityDbContext db, CancellationToken ct) =>
        {
            var tenants = await db.Tenants.AsNoTracking().ToListAsync(ct);
            return Results.Ok(tenants);
        })
            .WithName("GetTenants")
            .Produces<IEnumerable<Tenant>>(StatusCodes.Status200OK)
            .WithSummary("List all tenants")
            .WithDescription("Returns a collection of all registered tenants.");

        // GET /api/tenants/{id}
        _ = group.MapGet("/{id:guid}", async (Guid id, IdentityDbContext db, CancellationToken ct) =>
        {
            var tenant = await db.Tenants.Include(t => t.Indicators).FirstOrDefaultAsync(t => t.Id == id, ct);
            return tenant is not null
                ? Results.Ok(tenant)
                : Results.NotFound();
        })
            .WithName("GetTenant")
            .Produces<Tenant>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get a single tenant")
            .WithDescription("Returns the tenant identified by id.");

        // GET /api/tenants/by-owner/{ownerId}
        _ = group.MapGet("/by-owner/{ownerId:guid}", async (Guid ownerId, IdentityDbContext db, CancellationToken ct) =>
        {
            var tenants = await db.Tenants.AsNoTracking().Where(t => t.OwnerId == ownerId).ToListAsync(ct);
            return Results.Ok(tenants);
        })
            .WithName("GetTenantsByOwner")
            .Produces<IEnumerable<Tenant>>(StatusCodes.Status200OK)
            .WithSummary("List tenants by owner")
            .WithDescription("Returns a collection of tenants managed by the specified owner.");

        // POST /api/tenants
        _ = group.MapPost(string.Empty, async (CreateTenantRequest request, IdentityDbContext db, CancellationToken ct) =>
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                OwnerId = request.OwnerId,
                Name = request.Name,
                ClientId = request.ClientId,
                LogoUrl = request.LogoUrl,
                Description = request.Description,
                CreatedBy = request.CreatedBy
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/tenants/{tenant.Id}", tenant);
        })
            .WithName("CreateTenant")
            .Accepts<CreateTenantRequest>("application/json")
            .Produces<Tenant>(StatusCodes.Status201Created)
            .WithSummary("Create a new tenant")
            .WithDescription("Creates a new tenant and returns a 201 Created response.");

        // PUT /api/tenants/{id}
        _ = group.MapPut("/{id:guid}", async (Guid id, UpdateTenantRequest request, IdentityDbContext db, CancellationToken ct) =>
        {
            if (id != request.Id)
            {
                return Results.BadRequest($"Route id '{id}' does not match payload id '{request.Id}'.");
            }

            var tenant = await db.Tenants.FindAsync([id], ct);
            if (tenant is null)
            {
                return Results.NotFound();
            }

            tenant.OwnerId = request.OwnerId;
            tenant.Name = request.Name;
            tenant.ClientId = request.ClientId;
            tenant.LogoUrl = request.LogoUrl;
            tenant.Description = request.Description;
            tenant.ModifiedBy = request.ModifiedBy;
            tenant.ModifiedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return Results.Ok(tenant);
        })
            .WithName("UpdateTenant")
            .Accepts<UpdateTenantRequest>("application/json")
            .Produces<Tenant>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Update a tenant")
            .WithDescription("Updates the tenant identified by id.");

        // DELETE /api/tenants/{id}
        _ = group.MapDelete("/{id:guid}", async (Guid id, IdentityDbContext db, CancellationToken ct) =>
        {
            var tenant = await db.Tenants.FindAsync([id], ct);
            if (tenant is null)
            {
                return Results.NotFound();
            }

            db.Tenants.Remove(tenant);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
            .WithName("DeleteTenant")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Delete a tenant")
            .WithDescription("Removes the tenant identified by id.");
    }

    private static void MapOnDemandTlsEndpoint(IEndpointRouteBuilder app)
    {
        _ = app.MapGet("/allow", async (
            HttpRequest req,
            IdentityDbContext db,
            ILogger<IdentityDbContext> logger,
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
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .AllowAnonymous()
            .WithSummary("On-demand TLS certificate allowance check")
            .WithDescription("Returns 2xx if the domain is allowed for certificate issuance, 403 otherwise.");
    }
}
