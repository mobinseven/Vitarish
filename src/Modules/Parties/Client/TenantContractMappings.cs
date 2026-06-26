using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Parties.Client;

public static class TenantContractMappings
{
    public static TenantWriteDto ToWriteWithMetadata(this TenantReadDto tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        return new()
        {
            Id = tenant.Id,
            OwnerId = tenant.OwnerId,
            Name = tenant.Name,
            AppId = tenant.AppId,
            LogoUrl = tenant.LogoUrl,
            Description = tenant.Description,
            Metadata = new Dictionary<string, object?>(tenant.Metadata, StringComparer.OrdinalIgnoreCase)
        };
    }
}
