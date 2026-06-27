namespace Vitarish.Modules.Aggregator.Core.BackgroundJobs;

/// <summary>
/// Describes the tenant identity required to execute tenant-scoped background work.
/// </summary>
/// <param name="Id">Tenant identifier.</param>
/// <param name="OwnerId">Owner identifier.</param>
/// <param name="AppId">Application/client identifier.</param>
public sealed record BackgroundTenant(Guid Id, Guid OwnerId, string AppId);
