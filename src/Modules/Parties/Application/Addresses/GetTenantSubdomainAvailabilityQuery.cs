using MediatR;
using Microsoft.Extensions.Options;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Addresses;

namespace Vitarish.Parties.Application.Addresses;

public enum TenantSubdomainAvailabilityStatus
{
    Available = 1,
    Malformed = 2,
    Reserved = 3,
    Taken = 4
}

/// <param name="Subdomain">The normalized subdomain, or the trimmed input when it is malformed.</param>
/// <param name="Hostname">The full address when the subdomain is well formed and not reserved.</param>
public sealed record TenantSubdomainAvailability(string Subdomain, string? Hostname, TenantSubdomainAvailabilityStatus Status);

/// <summary>Read-only check of whether a subdomain can become a new tenant's address.</summary>
public sealed record GetTenantSubdomainAvailabilityQuery(string Subdomain) : IRequest<TenantSubdomainAvailability>;

public sealed class GetTenantSubdomainAvailabilityQueryHandler(
    ITenantHostnameRegistry registry,
    IOptions<TenantAddressOptions> options) : IRequestHandler<GetTenantSubdomainAvailabilityQuery, TenantSubdomainAvailability>
{
    public async Task<TenantSubdomainAvailability> Handle(GetTenantSubdomainAvailabilityQuery request, CancellationToken cancellationToken)
    {
        TenantAddressOptions addresses = options.Value;
        if (!addresses.IsConfigured)
            throw new InvalidOperationException("Tenant subdomains are unavailable because no tenant base domain is configured.");

        TenantSubdomainRejection rejection = TenantSubdomain.TryCreate(request.Subdomain, out TenantSubdomain? subdomain);
        if (subdomain is null)
        {
            TenantSubdomainAvailabilityStatus status = rejection == TenantSubdomainRejection.Reserved
                ? TenantSubdomainAvailabilityStatus.Reserved
                : TenantSubdomainAvailabilityStatus.Malformed;
            return new(request.Subdomain.Trim(), null, status);
        }

        string hostname = subdomain.ToHostname(addresses.TenantBaseDomain!);
        bool claimed = await registry.IsClaimedAsync(hostname, cancellationToken);
        return new(subdomain.Value, hostname, claimed ? TenantSubdomainAvailabilityStatus.Taken : TenantSubdomainAvailabilityStatus.Available);
    }
}
