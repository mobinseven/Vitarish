using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Endpoints;
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
        _ = group.MapGet(PartiesApiRoutes.UserTenants, GetUserTenantsAsync);
        _ = group.MapGet(PartiesApiRoutes.Owners, GetOwnersAsync);
        _ = group.MapGet(PartiesApiRoutes.OwnerById, GetOwnerAsync);
        _ = group.MapPost(PartiesApiRoutes.Owners, CreateOwnerAsync);
        _ = group.MapPut(PartiesApiRoutes.OwnerById, UpdateOwnerAsync);
        _ = group.MapDelete(PartiesApiRoutes.OwnerById, DeleteOwnerAsync);
        _ = group.MapGet(PartiesApiRoutes.Tenants, GetTenantsAsync);
        _ = group.MapGet(PartiesApiRoutes.TenantById, GetTenantAsync);
        _ = group.MapGet(PartiesApiRoutes.TenantsByOwner, GetTenantsByOwnerAsync);
        _ = group.MapPost(PartiesApiRoutes.Tenants, CreateTenantAsync);
        _ = group.MapPut(PartiesApiRoutes.TenantById, UpdateTenantAsync);
        _ = group.MapDelete(PartiesApiRoutes.TenantById, DeleteTenantAsync);
        _ = group.MapGet(PartiesApiRoutes.TenantIndicatorById, GetTenantIndicatorAsync);
        _ = group.MapGet(PartiesApiRoutes.TenantIndicatorsByTenant, GetTenantIndicatorsByTenantAsync);
        _ = group.MapPost(PartiesApiRoutes.TenantIndicators, CreateTenantIndicatorAsync);
        _ = group.MapPut(PartiesApiRoutes.TenantIndicatorById, UpdateTenantIndicatorAsync);
        _ = group.MapDelete(PartiesApiRoutes.TenantIndicatorById, DeleteTenantIndicatorAsync);
        _ = group.MapPost(PartiesApiRoutes.SetupTenant, SetupTenantAsync);
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
        D.TenantReadDto? tenant = await service.UpdateTenantAsync(ToDomain(request), cancellationToken);
        return tenant is null ? NotFound("Tenant was not found.") : Results.Ok(ToContract(tenant));
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
    private static async Task<IResult> CreateTenantIndicatorAsync(C.TenantIndicatorWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Value)) return BadRequest("Tenant indicator value is required.");
        D.TenantIndicatorReadDto? indicator = await service.CreateTenantIndicatorAsync(ToDomain(request), cancellationToken);
        return indicator is null ? Conflict("Tenant indicator could not be created.") : Results.Ok(ToContract(indicator));
    }
    private static async Task<IResult> UpdateTenantIndicatorAsync(Guid tenantIndicatorId, C.TenantIndicatorWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (tenantIndicatorId != request.Id) return BadRequest("Tenant indicator route id must match request id.");
        D.TenantIndicatorReadDto? indicator = await service.UpdateTenantIndicatorAsync(ToDomain(request), cancellationToken);
        return indicator is null ? NotFound("Tenant indicator was not found.") : Results.Ok(ToContract(indicator));
    }
    private static async Task<IResult> DeleteTenantIndicatorAsync(Guid tenantIndicatorId, IPartiesService service, CancellationToken cancellationToken)
    {
        await service.DeleteTenantIndicatorAsync(tenantIndicatorId, cancellationToken);
        return Results.Ok(true);
    }
    private static async Task<IResult> SetupTenantAsync(C.SetupTenantRequest request, IPartiesService service, CancellationToken cancellationToken)
    {
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
    private static C.UserTenantDto ToContract(M.UserTenant dto) => new(dto.Id, dto.OwnerId, dto.OwnerName, dto.Name, dto.AppId, dto.LogoUrl, dto.Description, dto.PrimaryIndicatorMethod is null ? null : (C.IndicatorMethod)dto.PrimaryIndicatorMethod, dto.PrimaryIndicatorValue);
    private static DE.VerificationInfo? ToDomain(C.VerificationInfoDto? dto) => dto is null ? null : new() { Method = (DEN.IndicatorVerificationMethod)dto.Method, Evidence = dto.Evidence, EvidenceExpectedHash = dto.EvidenceExpectedHash, Token = dto.Token };
    private static C.VerificationInfoDto? ToContract(DE.VerificationInfo? dto) => dto is null ? null : new() { Method = (C.IndicatorVerificationMethod)dto.Method, Evidence = dto.Evidence, EvidenceExpectedHash = dto.EvidenceExpectedHash, Token = dto.Token };
    private static IResult BadRequest(string message) => Results.BadRequest(new PartiesError(PartiesErrorCode.Validation, message));
    private static IResult NotFound(string message) => Results.NotFound(new PartiesError(PartiesErrorCode.NotFound, message));
    private static IResult Conflict(string message) => Results.Conflict(new PartiesError(PartiesErrorCode.Conflict, message));
}
