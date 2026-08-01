using Vitarish.Parties.Domain.Models;

namespace Vitarish.Parties.Application.Abstractions;

public interface IAppTenantProvisioner
{
    Task EnsureAsync(EnsureAppTenantRequest request, CancellationToken cancellationToken = default);
}
