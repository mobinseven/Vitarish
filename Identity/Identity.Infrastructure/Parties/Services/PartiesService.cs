using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Core.Parties.Application.Queries;
using Vitarish.Identity.Core.Parties.Domain.Entities;

namespace Vitarish.Identity.Infrastructure.Parties.Services;

public sealed class PartiesService(ISender sender) : IPartiesService
{
    public Task<Owner?> GetOwnerAsync(Guid id, CancellationToken ct = default) =>
        sender.Send(new GetOwner(id), ct);

    public Task<IEnumerable<Owner>> GetOwnersAsync(CancellationToken ct = default) =>
        sender.Send(new GetOwners(), ct);

    public Task<Result<Owner>> CreateOwnerAsync(CreateOwner command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task<Result> UpdateOwnerAsync(UpdateOwner command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task<Result> DeleteOwnerAsync(Guid id, CancellationToken ct = default) =>
        sender.Send(new DeleteOwner(id), ct);

    public Task<Tenant?> GetTenantAsync(Guid id, CancellationToken ct = default) =>
        sender.Send(new GetTenant(id), ct);

    public Task<IEnumerable<Tenant>> GetTenantsAsync(CancellationToken ct = default) =>
        sender.Send(new GetTenants(), ct);

    public Task<IEnumerable<Tenant>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        sender.Send(new GetTenantsByOwner(ownerId), ct);

    public Task<IEnumerable<Tenant>> GetTenantsByClientIdAsync(string clientId, CancellationToken ct = default) =>
        sender.Send(new GetTenantsByClientId(clientId), ct);

    public Task<Result<Tenant>> CreateTenantAsync(CreateTenant command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task<Result> UpdateTenantAsync(UpdateTenant command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task<Result> DeleteTenantAsync(Guid id, CancellationToken ct = default) =>
        sender.Send(new DeleteTenant(id), ct);

    public Task<IEnumerable<TenantIndicator>> GetTenantIndicatorsAsync(Guid tenantId, CancellationToken ct = default) =>
        sender.Send(new GetTenantIndicators(tenantId), ct);

    public Task<Result<TenantIndicator>> AddTenantIndicatorAsync(AddTenantIndicator command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task<Result> VerifyTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        sender.Send(new VerifyTenantIndicator(tenantId, indicatorId), ct);

    public Task<Result> ActivateTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        sender.Send(new ActivateTenantIndicator(tenantId, indicatorId), ct);

    public Task<Result> DisableTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        sender.Send(new DisableTenantIndicator(tenantId, indicatorId), ct);

    public Task<Result> DeleteTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        sender.Send(new DeleteTenantIndicator(tenantId, indicatorId), ct);
}
