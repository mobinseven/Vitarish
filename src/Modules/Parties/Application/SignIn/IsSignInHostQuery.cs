using MediatR;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Parties.Application.SignIn;

/// <summary>Whether the request came to the shared sign-in host, whose tenant is <see cref="SignInTenant"/>.</summary>
public sealed record IsSignInHostQuery : IRequest<bool>;

public sealed class IsSignInHostQueryHandler(IAppTenantDirectory tenants, IRequestContext requestContext)
    : IRequestHandler<IsSignInHostQuery, bool>
{
    public async Task<bool> Handle(IsSignInHostQuery request, CancellationToken cancellationToken) =>
        Guid.TryParse(requestContext.TenantId, out Guid tenantId)
        && await tenants.IsLiveAppTenantAsync(tenantId, SignInTenant.AppId, cancellationToken);
}
