
using MediatR;
using Vorn.Aaas.Parties.Application.Commands;
using Vorn.Aaas.Parties.Application.Queries;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Presentation.Abstractions;
using Vorn.Aaas.Parties.Presentation.Contracts;

namespace Vorn.Aaas.Parties.Infrastructure.Services;
/// <inheritdoc />
public sealed class PartiesService(ISender sender) : IPartiesService
{
    public Task<Owner?> GetOwnerAsync(Guid id, CancellationToken ct = default) =>
        sender.Send(new GetOwner(id), ct);

    public Task<IEnumerable<Owner>> GetOwnersAsync(CancellationToken ct = default) =>
        sender.Send(new GetOwners(), ct);

    public async Task<Owner?> CreateOwnerAsync(CreateOwnerRequest request, CancellationToken ct = default) =>
        await sender.Send(new CreateOwner(request.Name, request.LogoUrl, request.Description, request.CreatedBy), ct);

    public async Task<Owner?> UpdateOwnerAsync(UpdateOwnerRequest request, CancellationToken ct = default) =>
        await sender.Send(new UpdateOwner(request.Id, request.Name, request.LogoUrl, request.Description, request.ModifiedBy), ct);

    public Task DeleteOwnerAsync(Guid id, CancellationToken ct = default) =>
        sender.Send(new DeleteOwner(id), ct);

    public Task<Tenant?> GetTenantAsync(Guid id, CancellationToken ct = default) =>
        sender.Send(new GetTenant(id), ct);

    public Task<IEnumerable<Tenant>> GetTenantsAsync(CancellationToken ct = default) =>
        sender.Send(new GetTenants(), ct);

    public Task<IEnumerable<Tenant>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        sender.Send(new GetTenantsByOwner(ownerId), ct);

    public async Task<Tenant?> CreateTenantAsync(CreateTenantRequest request, CancellationToken ct = default) =>
        await sender.Send(new CreateTenant(request.OwnerId, request.Name, request.ClientId, request.LogoUrl, request.Description, request.CreatedBy), ct);

    public async Task<Tenant?> UpdateTenantAsync(UpdateTenantRequest request, CancellationToken ct = default) =>
        await sender.Send(new UpdateTenant(request.Id, request.OwnerId, request.Name, request.ClientId, request.LogoUrl, request.Description, request.ModifiedBy), ct);

    public Task DeleteTenantAsync(Guid id, CancellationToken ct = default) =>
        sender.Send(new DeleteTenant(id), ct);

    public Task<IEnumerable<TenantIndicator>> GetTenantIndicatorsAsync(Guid tenantId, CancellationToken ct = default) =>
        sender.Send(new GetTenantIndicators(tenantId), ct);

    public async Task<TenantIndicator?> AddTenantIndicatorAsync(AddTenantIndicatorRequest request, CancellationToken ct = default) =>
        await sender.Send(new AddTenantIndicator(request.TenantId, request.Method, request.Value, request.IsPrimary, request.VerificationMethod), ct);

    public Task<bool> VerifyTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        sender.Send(new VerifyTenantIndicator(tenantId, indicatorId), ct);

    public Task<bool> ActivateTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        sender.Send(new ActivateTenantIndicator(tenantId, indicatorId), ct);

    public Task<bool> DisableTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        sender.Send(new DisableTenantIndicator(tenantId, indicatorId), ct);

    public Task<bool> DeleteTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        sender.Send(new DeleteTenantIndicator(tenantId, indicatorId), ct);
}
