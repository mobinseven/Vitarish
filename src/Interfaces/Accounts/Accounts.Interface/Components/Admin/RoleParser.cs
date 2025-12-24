using Vitarish.ApiClients.Domain.Entities;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Accounts.Interface.Components.Admin;

internal static class RoleParser
{
    public static List<ParsedTenantRole> ParseTenantRoles(
        IEnumerable<string> roles,
        Dictionary<Guid, Tenant> tenantDict,
        Dictionary<Guid, Owner?> owners,
        Dictionary<string, ApiClient?> clients)
    {
        List<ParsedTenantRole> parsedRoles = [];

        foreach (string role in roles)
        {
            int separatorIndex = role.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            string tenantIdText = role[..separatorIndex];
            string roleName = role[(separatorIndex + 1)..];

            if (string.IsNullOrWhiteSpace(roleName) || !Guid.TryParse(tenantIdText, out Guid tenantId))
            {
                continue;
            }

            Tenant? tenant = tenantDict.GetValueOrDefault(tenantId);
            Owner? owner = tenant != null ? owners.GetValueOrDefault(tenant.OwnerId) : null;
            ApiClient? client = tenant != null && !string.IsNullOrWhiteSpace(tenant.ClientId)
                ? clients.GetValueOrDefault(tenant.ClientId)
                : null;

            string tenantName = tenant?.Name ?? tenantId.ToString();
            string ownerName = owner?.Name ?? (tenant?.OwnerId.ToString() ?? "-");
            string ownerId = tenant?.OwnerId.ToString() ?? "-";
            string clientId = client?.Id ?? (tenant?.ClientId ?? "-");

            parsedRoles.Add(new ParsedTenantRole(
                tenantId,
                tenantName,
                ownerId,
                ownerName,
                clientId,
                roleName,
                role));
        }

        return parsedRoles;
    }
}

internal sealed record ParsedTenantRole(
    Guid TenantId,
    string TenantName,
    string OwnerId,
    string OwnerName,
    string ClientId,
    string RoleName,
    string FullRoleName);
