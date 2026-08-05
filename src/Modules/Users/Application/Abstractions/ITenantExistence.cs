namespace Vitarish.Users.Application.Abstractions;

public interface ITenantExistence
{
    Task<bool> ExistsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
