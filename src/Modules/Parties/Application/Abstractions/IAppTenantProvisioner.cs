using Vitarish.Parties.Domain.Models;

namespace Vitarish.Parties.Application.Abstractions;

public interface IAppTenantProvisioner
{
    Task<Guid> EnsureAsync(EnsureAppTenantRequest request, CancellationToken cancellationToken = default);
}
