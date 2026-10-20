using Vitarish.Parties.Domain.Profiles;

namespace Vitarish.Parties.Application.Onboarding;

/// <summary>The account asking to create a tenant, as far as creation cares.</summary>
public sealed record TenantCreator(string UserId, bool HasVerifiedPhone);

public interface ITenantCreatorDirectory
{
    Task<TenantCreator?> FindAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>How many tenants the creator may already have created since <see cref="SinceUtc"/>.</summary>
public sealed record TenantCreationAllowance(DateTime SinceUtc, int Maximum);

/// <summary>A tenant to create in one step with its owner, primary address, roles, and owner role.</summary>
public sealed record NewSelfServiceTenant(
    Guid TenantId,
    Guid OwnerId,
    string CreatorUserId,
    string Name,
    string? Description,
    string Hostname,
    SelfServiceTenantProfile Profile,
    TenantCreationAllowance Allowance,
    string? Category = null,
    TenantBusinessDetails? BusinessDetails = null);

/// <summary>A tenant that self-service creation already made.</summary>
public sealed record ExistingSelfServiceTenant(
    Guid TenantId,
    string Name,
    string? Description,
    string? PrimaryHostname,
    string? Category = null,
    TenantBusinessDetails? BusinessDetails = null);

public enum TenantProvisioningOutcome
{
    Created = 1,
    AddressTaken = 2,
    LimitReached = 3
}

public interface ISelfServiceTenantStore
{
    Task<ExistingSelfServiceTenant?> FindAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the owner, tenant, and active primary address and assigns the creator as owner,
    /// so either all of it exists afterwards or none of it does. The address and the creator's
    /// allowance are checked in the same transaction as the writes.
    /// </summary>
    Task<TenantProvisioningOutcome> CreateAsync(NewSelfServiceTenant tenant, CancellationToken cancellationToken = default);
}
