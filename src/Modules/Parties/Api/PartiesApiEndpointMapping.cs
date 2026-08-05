using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Contracts.Errors;
using Vitarish.Parties.Contracts.Routes;
using C = Vitarish.Parties.Contracts.Tenants;
using D = Vitarish.Parties.Domain.Dtos;
using DE = Vitarish.Parties.Domain.Entities;
using DEN = Vitarish.Parties.Domain.Enums;
using M = Vitarish.Parties.Domain.Models;

namespace Vitarish.Parties.Api;

public static class PartiesApiEndpointMapping
{
    public static IEndpointRouteBuilder MapPartiesApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(PartiesApiRoutes.Base);
        _ = group.MapModuleGet(PartiesApiRoutes.UserTenants, GetUserTenantsAsync, "PartiesGetUserTenants");
        _ = group.MapModuleGet(PartiesApiRoutes.Owners, GetOwnersAsync, "PartiesGetOwners");
        _ = group.MapModuleGet(PartiesApiRoutes.OwnerById, GetOwnerAsync, "PartiesGetOwner");
        _ = group.MapModulePost(PartiesApiRoutes.Owners, CreateOwnerAsync, "PartiesCreateOwner");
        _ = group.MapModulePut(PartiesApiRoutes.OwnerById, UpdateOwnerAsync, "PartiesUpdateOwner");
        _ = group.MapModuleDelete(PartiesApiRoutes.OwnerById, DeleteOwnerAsync, "PartiesDeleteOwner");
        _ = group.MapModuleGet(PartiesApiRoutes.Tenants, GetTenantsAsync, "PartiesGetTenants");
        _ = group.MapModuleGet(PartiesApiRoutes.TenantById, GetTenantAsync, "PartiesGetTenant");
        _ = group.MapModuleGet(PartiesApiRoutes.TenantsByOwner, GetTenantsByOwnerAsync, "PartiesGetTenantsByOwner");
        _ = group.MapModulePost(PartiesApiRoutes.Tenants, CreateTenantAsync, "PartiesCreateTenant");
        _ = group.MapModulePut(PartiesApiRoutes.TenantById, UpdateTenantAsync, "PartiesUpdateTenant");
        _ = group.MapModuleDelete(PartiesApiRoutes.TenantById, DeleteTenantAsync, "PartiesDeleteTenant");
        _ = group.MapModuleGet(PartiesApiRoutes.TenantIndicatorById, GetTenantIndicatorAsync, "PartiesGetTenantIndicator");
        _ = group.MapModuleGet(PartiesApiRoutes.TenantIndicatorsByTenant, GetTenantIndicatorsByTenantAsync, "PartiesGetTenantIndicatorsByTenant");
        _ = group.MapModulePost(PartiesApiRoutes.TenantIndicators, CreateTenantIndicatorAsync, "PartiesCreateTenantIndicator");
        _ = group.MapModulePut(PartiesApiRoutes.TenantIndicatorById, UpdateTenantIndicatorAsync, "PartiesUpdateTenantIndicator");
        _ = group.MapModuleDelete(PartiesApiRoutes.TenantIndicatorById, DeleteTenantIndicatorAsync, "PartiesDeleteTenantIndicator");
        _ = group.MapModulePost(PartiesApiRoutes.SetupTenant, SetupTenantAsync, "PartiesSetupTenant");
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationOwners}", GetOwnersAsync, "PartiesAdministrationGetOwners").Produces<IEnumerable<C.OwnerReadDto>>(StatusCodes.Status200OK).WithMetadata(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationOwnerById}", GetOwnerAsync, "PartiesAdministrationGetOwner").Produces<C.OwnerReadDto>(StatusCodes.Status200OK).WithMetadata(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        _ = group.MapModulePost($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationOwners}", CreateOwnerAsync, "PartiesAdministrationCreateOwner").WithMetadata(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        _ = group.MapModulePut($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationOwnerById}", UpdateOwnerAsync, "PartiesAdministrationUpdateOwner").WithMetadata(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenants}", GetTenantsAsync, "PartiesAdministrationGetTenants").Produces<IEnumerable<C.TenantReadDto>>(StatusCodes.Status200OK).WithMetadata(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenantById}", GetTenantAsync, "PartiesAdministrationGetTenant").Produces<C.TenantReadDto>(StatusCodes.Status200OK).WithMetadata(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenantsByOwner}", GetTenantsByOwnerAsync, "PartiesAdministrationGetTenantsByOwner").WithMetadata(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        _ = group.MapModulePost($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenants}", CreateTenantAsync, "PartiesAdministrationCreateTenant").WithMetadata(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        _ = group.MapModulePut($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenantById}", UpdateAdministrationTenantAsync, "PartiesAdministrationUpdateTenant").WithMetadata(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenantIndicatorsByTenant}", GetTenantIndicatorsByTenantAsync, "PartiesAdministrationGetTenantIndicators").Produces<IEnumerable<C.TenantIndicatorReadDto>>(StatusCodes.Status200OK).WithMetadata(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenantIndicatorById}", GetTenantIndicatorAsync, "PartiesAdministrationGetTenantIndicator").Produces<C.TenantIndicatorReadDto>(StatusCodes.Status200OK).WithMetadata(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        _ = group.MapModulePost($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.TenantIndicators}", CreateAdministrationTenantIndicatorAsync, "PartiesAdministrationCreateTenantIndicator").WithMetadata(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        _ = group.MapModulePut($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenantIndicatorById}", UpdateAdministrationTenantIndicatorAsync, "PartiesAdministrationUpdateTenantIndicator").WithMetadata(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        return endpoints;
    }

    private static async Task<IResult> GetUserTenantsAsync(IPartiesService service, CancellationToken cancellationToken) =>
        Results.Ok((await service.GetUserTenantsAsync(cancellationToken)).Select(ToContract));
    private static async Task<IResult> GetOwnerAsync(Guid ownerId, IPartiesService service, CancellationToken cancellationToken) =>
        await service.GetOwnerAsync(ownerId, cancellationToken) is { } owner ? Results.Ok(ToContract(owner)) : NotFound("Owner was not found.");
    private static async Task<IResult> GetOwnersAsync(IPartiesService service, CancellationToken cancellationToken) =>
        Results.Ok((await service.GetOwnersAsync(cancellationToken)).Select(ToContract));
    private static async Task<IResult> CreateOwnerAsync(C.OwnerWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Owner name is required.");
        D.OwnerReadDto? owner = await service.CreateOwnerAsync(ToDomain(request), cancellationToken);
        return owner is null ? Conflict("Owner could not be created.") : Results.Ok(ToContract(owner));
    }
    private static async Task<IResult> UpdateOwnerAsync(Guid ownerId, C.OwnerWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (ownerId != request.Id) return BadRequest("Owner route id must match request id.");
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Owner name is required.");
        D.OwnerReadDto? owner = await service.UpdateOwnerAsync(ToDomain(request), cancellationToken);
        return owner is null ? NotFound("Owner was not found.") : Results.Ok(ToContract(owner));
    }
    private static async Task<IResult> DeleteOwnerAsync(Guid ownerId, IPartiesService service, CancellationToken cancellationToken)
    {
        await service.DeleteOwnerAsync(ownerId, cancellationToken);
        return Results.Ok(true);
    }
    private static async Task<IResult> GetTenantAsync(Guid tenantId, IPartiesService service, CancellationToken cancellationToken) =>
        await service.GetTenantAsync(tenantId, cancellationToken) is { } tenant ? Results.Ok(ToContract(tenant)) : NotFound("Tenant was not found.");
    private static async Task<IResult> GetTenantsAsync(IPartiesService service, CancellationToken cancellationToken) =>
        Results.Ok((await service.GetTenantsAsync(cancellationToken)).Select(ToContract));
    private static async Task<IResult> GetTenantsByOwnerAsync(Guid ownerId, IPartiesService service, CancellationToken cancellationToken) =>
        Results.Ok((await service.GetTenantsByOwnerAsync(ownerId, cancellationToken)).Select(ToContract));
    private static async Task<IResult> CreateTenantAsync(C.TenantWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.AppId)) return BadRequest("Tenant name and app id are required.");
        D.TenantReadDto? tenant = await service.CreateTenantAsync(ToDomain(request), cancellationToken);
        return tenant is null ? Conflict("Tenant could not be created.") : Results.Ok(ToContract(tenant));
    }
    private static async Task<IResult> UpdateTenantAsync(Guid tenantId, C.TenantWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (tenantId != request.Id) return BadRequest("Tenant route id must match request id.");
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.AppId)) return BadRequest("Tenant name and app id are required.");
        D.TenantReadDto? tenant = await service.UpdateTenantAsync(ToDomain(request), cancellationToken);
        return tenant is null ? NotFound("Tenant was not found.") : Results.Ok(ToContract(tenant));
    }
    private static async Task<IResult> UpdateAdministrationTenantAsync(Guid tenantId, C.TenantWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        D.TenantReadDto? existing = await service.GetTenantAsync(tenantId, cancellationToken);
        if (existing is null) return NotFound("Tenant was not found.");
        if (request.OwnerId != existing.OwnerId) return BadRequest("Tenant ownership cannot be changed through platform administration.");
        return await UpdateTenantAsync(tenantId, request, service, cancellationToken);
    }
    private static async Task<IResult> DeleteTenantAsync(Guid tenantId, IPartiesService service, CancellationToken cancellationToken)
    {
        await service.DeleteTenantAsync(tenantId, cancellationToken);
        return Results.Ok(true);
    }
    private static async Task<IResult> GetTenantIndicatorAsync(Guid tenantIndicatorId, IPartiesService service, CancellationToken cancellationToken) =>
        await service.GetTenantIndicatorAsync(tenantIndicatorId, cancellationToken) is { } indicator ? Results.Ok(ToContract(indicator)) : NotFound("Tenant indicator was not found.");
    private static async Task<IResult> GetTenantIndicatorsByTenantAsync(Guid tenantId, IPartiesService service, CancellationToken cancellationToken) =>
        Results.Ok((await service.GetTenantIndicatorsByTenantAsync(tenantId, cancellationToken)).Select(ToContract));
    private static async Task<IResult> CreateAdministrationTenantIndicatorAsync(C.TenantIndicatorWriteDto request, IPartiesService service, IConfiguration configuration, CancellationToken cancellationToken)
    {
        if (IsConfiguredLandingHostname(request.Value, request.Method, configuration)) return BadRequest("The configured Landing hostname can only be changed through the Landing binding workflow.");
        return await CreateTenantIndicatorAsync(request, service, cancellationToken);
    }
    private static async Task<IResult> CreateTenantIndicatorAsync(C.TenantIndicatorWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Value)) return BadRequest("Tenant indicator value is required.");
        D.TenantIndicatorReadDto? indicator = await service.CreateTenantIndicatorAsync(ToDomain(request), cancellationToken);
        return indicator is null ? Conflict("Tenant indicator could not be created.") : Results.Ok(ToContract(indicator));
    }
    private static async Task<IResult> UpdateAdministrationTenantIndicatorAsync(Guid tenantIndicatorId, C.TenantIndicatorWriteDto request, IPartiesService service, IConfiguration configuration, CancellationToken cancellationToken)
    {
        D.TenantIndicatorReadDto? existing = await service.GetTenantIndicatorAsync(tenantIndicatorId, cancellationToken);
        if (existing is not null && existing.TenantId != request.TenantId) return BadRequest("Tenant indicator ownership cannot be changed through platform administration.");
        if (existing is not null && IsConfiguredLandingHostname(existing.Value, (C.IndicatorMethod)existing.Method, configuration)) return BadRequest("The configured Landing hostname can only be changed through the Landing binding workflow.");
        if (IsConfiguredLandingHostname(request.Value, request.Method, configuration)) return BadRequest("The configured Landing hostname can only be changed through the Landing binding workflow.");
        return await UpdateTenantIndicatorAsync(tenantIndicatorId, request, service, cancellationToken);
    }
    private static async Task<IResult> UpdateTenantIndicatorAsync(Guid tenantIndicatorId, C.TenantIndicatorWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (tenantIndicatorId != request.Id) return BadRequest("Tenant indicator route id must match request id.");
        if (string.IsNullOrWhiteSpace(request.Value)) return BadRequest("Tenant indicator value is required.");
        D.TenantIndicatorReadDto? indicator = await service.UpdateTenantIndicatorAsync(ToDomain(request), cancellationToken);
        return indicator is null ? NotFound("Tenant indicator was not found.") : Results.Ok(ToContract(indicator));
    }

    private static bool IsConfiguredLandingHostname(string value, C.IndicatorMethod method, IConfiguration configuration)
    {
        if (method != C.IndicatorMethod.Hostname) return false;
        string? configured = configuration["Vitarish:LandingUrl"];
        return Uri.TryCreate(configured, UriKind.Absolute, out Uri? uri) &&
            string.Equals(uri.Host.TrimEnd('.'), value.Trim().TrimEnd('.'), StringComparison.OrdinalIgnoreCase);
    }
    private static async Task<IResult> DeleteTenantIndicatorAsync(Guid tenantIndicatorId, IPartiesService service, CancellationToken cancellationToken)
    {
        await service.DeleteTenantIndicatorAsync(tenantIndicatorId, cancellationToken);
        return Results.Ok(true);
    }
    private static async Task<IResult> SetupTenantAsync(C.SetupTenantRequest request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OwnerName) || string.IsNullOrWhiteSpace(request.TenantName) || string.IsNullOrWhiteSpace(request.AppId)) return BadRequest("Owner name, tenant name, and app id are required.");
        D.SetupTenantRequest domain = new(request.OwnerName, request.TenantName, request.AppId, (DEN.IndicatorMethod)request.IndicatorMethod, request.IndicatorValue, (DEN.IndicatorStatus)request.IndicatorStatus, request.SelectedModules, request.UserId);
        D.TenantReadDto? tenant = await service.SetupTenantAsync(domain, cancellationToken);
        return tenant is null ? Conflict("Tenant setup could not be completed.") : Results.Ok(ToContract(tenant));
    }
    private static D.OwnerWriteDto ToDomain(C.OwnerWriteDto dto) => new() { Id = dto.Id, Name = dto.Name, LogoUrl = dto.LogoUrl, Description = dto.Description };
    private static D.TenantWriteDto ToDomain(C.TenantWriteDto dto) => new() { Id = dto.Id, OwnerId = dto.OwnerId, Name = dto.Name, LogoUrl = dto.LogoUrl, Description = dto.Description, AppId = dto.AppId, Metadata = dto.Metadata };
    private static D.TenantIndicatorWriteDto ToDomain(C.TenantIndicatorWriteDto dto) => new() { Id = dto.Id, TenantId = dto.TenantId, Method = (DEN.IndicatorMethod)dto.Method, Value = dto.Value, Status = (DEN.IndicatorStatus)dto.Status, IsPrimary = dto.IsPrimary, Verification = ToDomain(dto.Verification), VerifiedAt = dto.VerifiedAt, LastCheckedAt = dto.LastCheckedAt };
    private static C.OwnerReadDto ToContract(D.OwnerReadDto dto) => new() { Id = dto.Id, Name = dto.Name, LogoUrl = dto.LogoUrl, Description = dto.Description };
    private static C.TenantReadDto ToContract(D.TenantReadDto dto) => new() { Id = dto.Id, OwnerId = dto.OwnerId, Name = dto.Name, LogoUrl = dto.LogoUrl, Description = dto.Description, AppId = dto.AppId, Metadata = dto.Metadata };
    private static C.TenantIndicatorReadDto ToContract(D.TenantIndicatorReadDto dto) => new() { Id = dto.Id, TenantId = dto.TenantId, Method = (C.IndicatorMethod)dto.Method, Value = dto.Value, Status = (C.IndicatorStatus)dto.Status, IsPrimary = dto.IsPrimary, Verification = ToContract(dto.Verification), VerifiedAt = dto.VerifiedAt, LastCheckedAt = dto.LastCheckedAt };
    private static C.UserTenantDto ToContract(M.UserTenant dto) => new(dto.TenantId, Guid.Empty, null, dto.TenantName, dto.TenantAppId, dto.TenantLogoUrl, dto.TenantDescription, null, dto.IndicatorValue);
    private static DE.VerificationInfo? ToDomain(C.VerificationInfoDto? dto) => dto is null ? null : new() { Method = (DEN.IndicatorVerificationMethod)dto.Method, Evidence = dto.Evidence, EvidenceExpectedHash = dto.EvidenceExpectedHash, Token = dto.Token };
    private static C.VerificationInfoDto? ToContract(DE.VerificationInfo? dto) => dto is null ? null : new() { Method = (C.IndicatorVerificationMethod)dto.Method, Evidence = dto.Evidence, EvidenceExpectedHash = dto.EvidenceExpectedHash, Token = dto.Token };
    private static IResult BadRequest(string message) => Results.BadRequest(new PartiesError(PartiesErrorCode.Validation, message));
    private static IResult NotFound(string message) => Results.NotFound(new PartiesError(PartiesErrorCode.NotFound, message));
    private static IResult Conflict(string message) => Results.Conflict(new PartiesError(PartiesErrorCode.Conflict, message));
}
