using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Apps;
using Vitarish.Parties.Application.SignIn;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>Reads a user's workspaces from their Users tenant roles and the Parties tenants they name.</summary>
public sealed class WorkspaceDirectory(ISender sender, PartiesDbContext dbContext, IPlatformAppCatalog appCatalog) : IWorkspaceDirectory
{
    public async Task<IReadOnlyList<WorkspaceDirectoryEntry>> ListAsync(string userId, CancellationToken cancellationToken = default)
    {
        string[] workspaceAppIds = [.. appCatalog.Apps.Where(app => app.HostsWorkspaces).Select(app => app.AppId).Distinct(StringComparer.Ordinal)];
        if (workspaceAppIds.Length == 0)
            return [];

        IEnumerable<IdentityRole> roles = await sender.Send(new GetUserRolesQuery(userId), cancellationToken);
        Dictionary<Guid, string> roleByTenant = [];
        foreach (string? roleName in roles.Select(role => role.Name))
        {
            if (!TenantRoleDefinitions.IsTenantRoleName(roleName))
                continue;
            string[] parts = roleName!.Split(TenantRoleDefinitions.RoleSeparator, 2, StringSplitOptions.TrimEntries);
            Guid tenantId = Guid.Parse(parts[0]);
            // An owner who is also a user of the same tenant is listed once, as its owner.
            if (!roleByTenant.TryGetValue(tenantId, out string? existing) || existing != IdentityDefinitions.TenantOwner)
                roleByTenant[tenantId] = parts[1];
        }

        if (roleByTenant.Count == 0)
            return [];

        Guid[] tenantIds = [.. roleByTenant.Keys];
        var hosts = await dbContext.TenantIndicators
            .AsNoTracking()
            .Where(indicator => tenantIds.Contains(indicator.TenantId)
                && workspaceAppIds.Contains(indicator.Tenant.AppId)
                && indicator.IsPrimary
                && indicator.Status == IndicatorStatus.Active
                && indicator.Tenant.DeletedAt == null)
            .Select(indicator => new { indicator.TenantId, indicator.Tenant.AppId, indicator.Tenant.Name, indicator.Value, HasLogo = indicator.Tenant.LogoUrl != null })
            .ToListAsync(cancellationToken);

        return [.. hosts
            .GroupBy(host => host.TenantId)
            .Select(group => group.First())
            .Select(host => new WorkspaceDirectoryEntry(host.TenantId, host.AppId, host.Name, host.Value, roleByTenant[host.TenantId], host.HasLogo))];
    }
}
