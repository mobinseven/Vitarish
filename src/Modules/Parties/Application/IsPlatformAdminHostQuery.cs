using MediatR;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Parties.Application;

/// <summary>Whether the request came to the admin host, whose tenant is <see cref="PlatformAdminTenant"/>.</summary>
public sealed record IsPlatformAdminHostQuery : IRequest<bool>;

public sealed class IsPlatformAdminHostQueryHandler(IAppTenantDirectory tenants, IRequestContext requestContext)
    : IRequestHandler<IsPlatformAdminHostQuery, bool>
{
    public async Task<bool> Handle(IsPlatformAdminHostQuery request, CancellationToken cancellationToken) =>
        Guid.TryParse(requestContext.TenantId, out Guid tenantId)
        && await tenants.IsLiveAppTenantAsync(tenantId, PlatformAdminTenant.AppId, cancellationToken);
}
