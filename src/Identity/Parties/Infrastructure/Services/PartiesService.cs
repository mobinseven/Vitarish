
using MediatR;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Application.Queries;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class PartiesService(ISender sender) : IPartiesService
{
    public Task<Owner?> GetOwnerAsync(Guid id, CancellationToken ct = default) =>
        sender.Send(new GetOwner(id), ct);

    public Task<IEnumerable<Owner>> GetOwnersAsync(CancellationToken ct = default) =>
        sender.Send(new GetOwners(), ct);

    public async Task<Owner?> CreateOwnerAsync(CreateOwner request, CancellationToken ct = default) =>
        await sender.Send(request, ct);

    public async Task<Owner?> UpdateOwnerAsync(UpdateOwner request, CancellationToken ct = default) =>
        await sender.Send(request, ct);

    public Task DeleteOwnerAsync(Guid id, CancellationToken ct = default) =>
        sender.Send(new DeleteOwner(id), ct);

    public Task<Tenant?> GetTenantAsync(Guid id, CancellationToken ct = default) =>
        sender.Send(new GetTenant(id), ct);

    public Task<IEnumerable<Tenant>> GetTenantsAsync(CancellationToken ct = default) =>
        sender.Send(new GetTenants(), ct);

    public Task<IEnumerable<Tenant>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        sender.Send(new GetTenantsByOwner(ownerId), ct);

    public async Task<Tenant?> CreateTenantAsync(CreateTenant request, CancellationToken ct = default) =>
        await sender.Send(request, ct);

    public async Task<Tenant?> UpdateTenantAsync(UpdateTenant request, CancellationToken ct = default) =>
        await sender.Send(request, ct);

    public Task DeleteTenantAsync(Guid id, CancellationToken ct = default) =>
        sender.Send(new DeleteTenant(id), ct);

    public Task<IEnumerable<TenantIndicator>> GetTenantIndicatorsAsync(Guid tenantId, CancellationToken ct = default) =>
        sender.Send(new GetTenantIndicators(tenantId), ct);

    public async Task<TenantIndicator?> AddTenantIndicatorAsync(AddTenantIndicator request, CancellationToken ct = default) =>
        await sender.Send(request, ct);

    public Task<bool> VerifyTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        sender.Send(new VerifyTenantIndicator(tenantId, indicatorId), ct);

    public Task<bool> ActivateTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        sender.Send(new ActivateTenantIndicator(tenantId, indicatorId), ct);

    public Task<bool> DisableTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        sender.Send(new DisableTenantIndicator(tenantId, indicatorId), ct);

    public Task<bool> DeleteTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        sender.Send(new DeleteTenantIndicator(tenantId, indicatorId), ct);
}
