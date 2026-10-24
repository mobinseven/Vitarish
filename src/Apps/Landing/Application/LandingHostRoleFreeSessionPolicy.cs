using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Parties.Application;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Landing.Application;

/// <summary>
/// Lets a person without a clinic stay signed in on the landing host, so quick sign-in can happen
/// inside the landing conversation (ADR-0017). It admits the session only when the request's tenant
/// is the live landing tenant, which holds no practice data; a landing host rebound to any other
/// tenant admits nothing.
/// </summary>
public sealed class LandingHostRoleFreeSessionPolicy(IAppTenantDirectory tenants, IRequestContext requestContext) : IRoleFreeSessionPolicy
{
    public async Task<bool> AllowsRoleFreeSessionAsync(CancellationToken cancellationToken = default) =>
        Guid.TryParse(requestContext.TenantId, out Guid tenantId)
        && await tenants.IsLiveAppTenantAsync(tenantId, LandingTenant.AppId, cancellationToken);
}
