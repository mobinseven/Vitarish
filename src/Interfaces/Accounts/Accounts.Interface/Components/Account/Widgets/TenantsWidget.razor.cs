using Vitarish.Interfaces.Base.Common;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Dtos;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Accounts.Interface.Components.Account.Widgets;

public partial class TenantsWidget : CommonBase
{
    private readonly List<TenantAccessEntry> tenantEntries = [];

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IPartiesService PartiesService { get; set; } = default!;

    protected override async Task ReloadRequired()
    {
        if(RequestContext.UserId is null)
        {
            throw new ArgumentNullException(nameof(RequestContext.UserId));
        }

        IEnumerable<IdentityRole> roles = await UserService.GetUserRolesAsync(RequestContext.UserId);
        List<TenantRoleAssignment> tenantRoleAssignments = ParseTenantRoles(roles.Select(ur => ur.Name).Cast<string>());

        if (tenantRoleAssignments.Count == 0)
        {
            return;
        }

        List<Guid> tenantIds = [.. tenantRoleAssignments
                .Select(role => role.TenantId)
                .Distinct()];

        Dictionary<Guid, Task<TenantReadDto?>> tenantTasks = tenantIds
            .ToDictionary(id => id, id => PartiesService.GetTenantAsync(id));
        _ = await Task.WhenAll(tenantTasks.Values);

        Dictionary<Guid, TenantReadDto?> tenants = tenantTasks
            .ToDictionary(entry => entry.Key, entry => entry.Value.Result);

        // Load indicators for all tenants
        Dictionary<Guid, Task<IEnumerable<TenantIndicatorReadDto>>> indicatorTasks = tenantIds
            .ToDictionary(id => id, id => PartiesService.GetTenantIndicatorsByTenantAsync(id));
        _ = await Task.WhenAll(indicatorTasks.Values);

        Dictionary<Guid, IEnumerable<TenantIndicatorReadDto>> tenantIndicators = indicatorTasks
            .ToDictionary(entry => entry.Key, entry => entry.Value.Result);

        foreach (IGrouping<Guid, TenantRoleAssignment> grouping in tenantRoleAssignments.GroupBy(role => role.TenantId))
        {
            TenantReadDto? tenant = tenants.GetValueOrDefault(grouping.Key);
            IEnumerable<TenantIndicatorReadDto> indicators = tenantIndicators.GetValueOrDefault(grouping.Key) ?? [];

            if (tenant is null)
            {
                continue;
            }

            TenantIndicatorReadDto? primaryIndicator = indicators
                .OrderByDescending(indicator => indicator.IsPrimary)
                .FirstOrDefault();

            if (primaryIndicator is null)
            {
                continue;
            }

            List<string> roleDisplay = [.. grouping.Select(role => role.DisplayName)];

            tenantEntries.Add(new TenantAccessEntry(
                grouping.Key,
                tenant,
                primaryIndicator));
        }
    }

    private void NavigateToTenant(TenantAccessEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.PrimaryUrl))
        {
            return;
        }

        NavigationManager.NavigateTo(entry.PrimaryUrl, forceLoad: true);
    }

    private static List<TenantRoleAssignment> ParseTenantRoles(IEnumerable<string> roles)
    {
        List<TenantRoleAssignment> assignments = [];

        foreach (string role in roles)
        {
            int separatorIndex = role.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            string tenantIdText = role[..separatorIndex];
            string roleName = role[(separatorIndex + 1)..];

            if (string.IsNullOrWhiteSpace(roleName))
            {
                continue;
            }

            if (!Guid.TryParse(tenantIdText, out Guid tenantId))
            {
                continue;
            }

            assignments.Add(new TenantRoleAssignment(tenantId, roleName, role));
        }

        return assignments;
    }

    private sealed record TenantRoleAssignment(Guid TenantId, string RoleName, string DisplayName);

    private sealed record TenantAccessEntry(
        Guid TenantId,
        TenantReadDto Tenant,
        TenantIndicatorReadDto PrimaryIndicator)
    {
        public string TenantDisplayName => string.IsNullOrWhiteSpace(Tenant?.Name)
            ? $"Tenant {TenantId.ToString()[..8]}"
            : Tenant!.Name;

        public string PrimaryIdentifier => PrimaryIndicator?.Value ?? string.Empty;
        
        public string PrimaryUrl => PrimaryIndicator.Method switch
        {
            IndicatorMethod.Subdomain => PrimaryIndicator.Value ?? throw new InvalidDataException("primary indicator value is null"),
            _ => Tenant.ClientId
        };
    }
}
