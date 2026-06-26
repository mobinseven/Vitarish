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
namespace Vitarish.Parties.Api;
public static class PartiesApiEndpointMapping
{
    public static IEndpointRouteBuilder MapPartiesApi(this IEndpointRouteBuilder endpoints){ var group=endpoints.MapProtectedModuleGroup(PartiesApiRoutes.Base); _=group.MapPost(PartiesApiRoutes.Tenants, CreateTenantAsync); _=group.MapPut(PartiesApiRoutes.TenantById, UpdateTenantAsync); _=group.MapPost(PartiesApiRoutes.TenantIndicators, CreateTenantIndicatorAsync); return endpoints; }
    private static async Task<IResult> CreateTenantAsync(C.TenantWriteDto request, IPartiesService service, CancellationToken cancellationToken){ if(string.IsNullOrWhiteSpace(request.Name)||string.IsNullOrWhiteSpace(request.AppId)) return Results.BadRequest(new PartiesError(PartiesErrorCode.Validation,"Tenant name and app id are required.")); D.TenantReadDto? tenant=await service.CreateTenantAsync(ToDomain(request), cancellationToken); return tenant is null?Results.Conflict(new PartiesError(PartiesErrorCode.Conflict,"Tenant could not be created.")):Results.Ok(ToContract(tenant)); }
    private static async Task<IResult> UpdateTenantAsync(Guid tenantId, C.TenantWriteDto request, IPartiesService service, CancellationToken cancellationToken){ if(tenantId!=request.Id) return Results.BadRequest(new PartiesError(PartiesErrorCode.Validation,"Tenant route id must match request id.")); D.TenantReadDto? tenant=await service.UpdateTenantAsync(ToDomain(request), cancellationToken); return tenant is null?Results.NotFound(new PartiesError(PartiesErrorCode.NotFound,"Tenant was not found.")):Results.Ok(ToContract(tenant)); }
    private static async Task<IResult> CreateTenantIndicatorAsync(C.TenantIndicatorWriteDto request, IPartiesService service, CancellationToken cancellationToken){ if(string.IsNullOrWhiteSpace(request.Value)) return Results.BadRequest(new PartiesError(PartiesErrorCode.Validation,"Tenant indicator value is required.")); D.TenantIndicatorReadDto? indicator=await service.CreateTenantIndicatorAsync(ToDomain(request), cancellationToken); return indicator is null?Results.Conflict(new PartiesError(PartiesErrorCode.Conflict,"Tenant indicator could not be created.")):Results.Ok(ToContract(indicator)); }
    private static D.TenantWriteDto ToDomain(C.TenantWriteDto dto)=>new(){ Id=dto.Id, OwnerId=dto.OwnerId, Name=dto.Name, LogoUrl=dto.LogoUrl, Description=dto.Description, AppId=dto.AppId, Metadata=dto.Metadata };
    private static D.TenantIndicatorWriteDto ToDomain(C.TenantIndicatorWriteDto dto)=>new(){ Id=dto.Id, TenantId=dto.TenantId, Method=(DEN.IndicatorMethod)dto.Method, Value=dto.Value, Status=(DEN.IndicatorStatus)dto.Status, IsPrimary=dto.IsPrimary, Verification=ToDomain(dto.Verification), VerifiedAt=dto.VerifiedAt, LastCheckedAt=dto.LastCheckedAt };
    private static C.TenantReadDto ToContract(D.TenantReadDto dto)=>new(){ Id=dto.Id, OwnerId=dto.OwnerId, Name=dto.Name, LogoUrl=dto.LogoUrl, Description=dto.Description, AppId=dto.AppId, Metadata=dto.Metadata };
    private static C.TenantIndicatorReadDto ToContract(D.TenantIndicatorReadDto dto)=>new(){ Id=dto.Id, TenantId=dto.TenantId, Method=(C.IndicatorMethod)dto.Method, Value=dto.Value, Status=(C.IndicatorStatus)dto.Status, IsPrimary=dto.IsPrimary, Verification=ToContract(dto.Verification), VerifiedAt=dto.VerifiedAt, LastCheckedAt=dto.LastCheckedAt };
    private static DE.VerificationInfo? ToDomain(C.VerificationInfoDto? dto)=> dto is null?null:new(){ Method=(DEN.IndicatorVerificationMethod)dto.Method, Evidence=dto.Evidence, EvidenceExpectedHash=dto.EvidenceExpectedHash, Token=dto.Token };
    private static C.VerificationInfoDto? ToContract(DE.VerificationInfo? dto)=> dto is null?null:new(){ Method=(C.IndicatorVerificationMethod)dto.Method, Evidence=dto.Evidence, EvidenceExpectedHash=dto.EvidenceExpectedHash, Token=dto.Token };
}
