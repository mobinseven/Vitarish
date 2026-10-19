using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Extensions;
using Vitarish.Users.Application.Commands;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>
/// Creates a self-service tenant across Parties and Users without leaving part of it behind.
/// The Parties rows are written in a serializable transaction that commits last, after Users
/// has made the creator the owner. If Users fails, the Parties transaction rolls back and the
/// owner assignment is undone; if the Parties commit fails, the owner assignment is undone. A
/// crash between the two leaves only roles for a tenant id that never existed, which nothing
/// can resolve or sign in to. A role is never assigned while its user has an active session,
/// so the creator's sessions end first, as replaced by the session they open on the new
/// tenant's own address. The whole attempt runs under the context's execution strategy.
/// </summary>
public sealed class SelfServiceTenantStore(
    PartiesDbContext dbContext,
    ISender sender,
    IAuthenticationSessionRegistry sessions,
    ILogger<SelfServiceTenantStore> logger) : ISelfServiceTenantStore
{
    public async Task<ExistingSelfServiceTenant?> FindAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await dbContext.Tenants.AsNoTracking()
            .Include(item => item.Indicators)
            .SingleOrDefaultAsync(item => item.Id == tenantId && item.DeletedAt == null, cancellationToken);
        return tenant is null
            ? null
            : new ExistingSelfServiceTenant(
                tenant.Id,
                tenant.Name,
                tenant.Description,
                tenant.Indicators.SingleOrDefault(indicator => indicator.IsPrimary && indicator.DeletedAt == null)?.Value);
    }

    public Task<TenantProvisioningOutcome> CreateAsync(NewSelfServiceTenant tenant, CancellationToken cancellationToken = default)
    {
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() => CreateOnceAsync(tenant, cancellationToken));
    }

    private async Task<TenantProvisioningOutcome> CreateOnceAsync(NewSelfServiceTenant tenant, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        string hostname = HostnameNormalizer.Canonical(tenant.Hostname);
        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        // Read inside the serializable transaction, so concurrent requests cannot all pass the limit.
        int created = await dbContext.Tenants.AsNoTracking().CountAsync(
            item => item.CreatedBy == tenant.CreatorUserId && item.CreatedAt >= tenant.Allowance.SinceUtc && item.DeletedAt == null,
            cancellationToken);
        if (created >= tenant.Allowance.Maximum)
            return TenantProvisioningOutcome.LimitReached;
        if (await IsClaimedAsync(hostname, cancellationToken))
            return TenantProvisioningOutcome.AddressTaken;

        Owner owner = new() { Id = tenant.OwnerId, Name = tenant.Name };
        Tenant createdTenant = new()
        {
            Id = tenant.TenantId,
            OwnerId = tenant.OwnerId,
            Name = tenant.Name,
            Description = tenant.Description,
            AppId = tenant.Profile.AppId
        };
        TenantIndicator address = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            Method = IndicatorMethod.Subdomain,
            Value = hostname,
            Status = IndicatorStatus.Active,
            IsPrimary = true
        };
        dbContext.Owners.Add(owner);
        dbContext.Tenants.Add(createdTenant);
        dbContext.TenantIndicators.Add(address);

        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (transaction is not null)
        {
            // A concurrent request claimed the address between the check and the insert.
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            if (await IsClaimedAsync(hostname, cancellationToken))
                return TenantProvisioningOutcome.AddressTaken;
            throw;
        }

        try
        {
            _ = await sessions.RevokeActiveSessionsAsync(tenant.CreatorUserId, AuthenticationSessionTerminationReason.ReplacedByNewSession, cancellationToken);
            AssignNewTenantOwnerResult assignment = await sender.Send(new AssignNewTenantOwnerCommand(tenant.CreatorUserId, tenant.TenantId), cancellationToken);
            if (assignment is not (AssignNewTenantOwnerResult.Assigned or AssignNewTenantOwnerResult.AlreadyOwner))
                throw new InvalidOperationException($"The new tenant's owner could not be assigned: {assignment}.");
        }
        catch
        {
            await DiscardAsync(transaction, owner, createdTenant, address);
            // Users may have committed before failing (for example, while refreshing its cache).
            await RevokeOwnerAsync(tenant);
            throw;
        }

        if (transaction is null)
            return TenantProvisioningOutcome.Created;

        try
        {
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RevokeOwnerAsync(tenant);
            throw;
        }

        return TenantProvisioningOutcome.Created;
    }

    private Task<bool> IsClaimedAsync(string hostname, CancellationToken cancellationToken) =>
        dbContext.TenantIndicators.AsNoTracking().AnyAsync(indicator => indicator.Value == hostname && indicator.DeletedAt == null, cancellationToken);

    // Without a relational transaction (the in-memory E2E store) the rows were already saved;
    // removing them soft-deletes them, which every query here ignores.
    private async Task DiscardAsync(IDbContextTransaction? transaction, Owner owner, Tenant tenant, TenantIndicator address)
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return;
        }

        dbContext.TenantIndicators.Remove(address);
        dbContext.Tenants.Remove(tenant);
        dbContext.Owners.Remove(owner);
        _ = await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    private async Task RevokeOwnerAsync(NewSelfServiceTenant tenant)
    {
        try
        {
            await sender.Send(new RevokeNewTenantOwnerCommand(tenant.CreatorUserId, tenant.TenantId), CancellationToken.None);
        }
        catch (Exception exception)
        {
            // The tenant does not exist, so its leftover roles grant nothing; record them for cleanup.
            logger.LogError(exception, "Could not undo the owner assignment for tenant {TenantId} after its creation failed.", tenant.TenantId);
        }
    }
}
