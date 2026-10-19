namespace Vitarish.Parties.Application.Addresses;

/// <summary>
/// Where self-service tenant addresses live (<c>Vitarish:TenantBaseDomain</c>). A tenant
/// claiming subdomain <c>x</c> is served at <c>x.&lt;TenantBaseDomain&gt;</c>; when unset,
/// tenants cannot claim addresses on their own.
/// </summary>
public sealed class TenantAddressOptions
{
    public string? TenantBaseDomain { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(TenantBaseDomain);
}
