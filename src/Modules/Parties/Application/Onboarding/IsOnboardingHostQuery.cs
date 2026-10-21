using MediatR;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Parties.Application.Onboarding;

/// <summary>Whether the request came to the onboarding host, whose tenant is <see cref="OnboardingTenant"/>.</summary>
public sealed record IsOnboardingHostQuery : IRequest<bool>;

public sealed class IsOnboardingHostQueryHandler(IAppTenantDirectory tenants, IRequestContext requestContext)
    : IRequestHandler<IsOnboardingHostQuery, bool>
{
    public async Task<bool> Handle(IsOnboardingHostQuery request, CancellationToken cancellationToken) =>
        Guid.TryParse(requestContext.TenantId, out Guid tenantId)
        && await tenants.IsLiveAppTenantAsync(tenantId, OnboardingTenant.AppId, cancellationToken);
}
