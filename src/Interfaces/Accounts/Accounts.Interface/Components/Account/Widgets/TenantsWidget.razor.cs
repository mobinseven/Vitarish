using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Accounts.Interface.Components.Account.Widgets;

public partial class TenantsWidget : IdentityCommonBase
{
    private readonly List<TenantAccessEntry> tenantEntries = [];

    [Inject] private IPartiesService PartiesService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        tenantEntries.Clear();
        StateHasChanged();

        try
        {
            IEnumerable<IdentityRole> roles = await UserService.GetUserRolesAsync(User.Id);
            List<TenantRoleAssignment> tenantRoleAssignments = ParseTenantRoles(roles.Select(ur => ur.Name).Cast<string>());

            if (tenantRoleAssignments.Count == 0)
            {
                return;
            }

            List<Guid> tenantIds = [.. tenantRoleAssignments
                .Select(role => role.TenantId)
                .Distinct()];

            Dictionary<Guid, Task<Tenant?>> tenantTasks = tenantIds
                .ToDictionary(id => id, id => PartiesService.GetTenantAsync(id));
            _ = await Task.WhenAll(tenantTasks.Values);

            Dictionary<Guid, Tenant?> tenants = tenantTasks
                .ToDictionary(entry => entry.Key, entry => entry.Value.Result);

            foreach (IGrouping<Guid, TenantRoleAssignment> grouping in tenantRoleAssignments.GroupBy(role => role.TenantId))
            {
                Tenant? tenant = tenants.GetValueOrDefault(grouping.Key);
                TenantIndicator? primaryIndicator = null;

                if (tenant is not null)
                {
                    // Get indicators from the tenant entity itself
                    primaryIndicator = tenant.Indicators
                        .OrderByDescending(indicator => indicator.IsPrimary)
                        .FirstOrDefault();
                }

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
        finally
        {
            StateHasChanged();
        }
    }

    private void NavigateToTenant(TenantAccessEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.PrimaryIdentifier))
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
        Tenant? Tenant,
        TenantIndicator? PrimaryIndicator)
    {
        public string TenantDisplayName => string.IsNullOrWhiteSpace(Tenant?.Name)
            ? $"Tenant {TenantId.ToString()[..8]}"
            : Tenant!.Name;

        public string PrimaryIdentifier => PrimaryIndicator?.Value ?? string.Empty;
        public string PrimaryUrl => PrimaryIndicator.Method switch
        {
            Parties.Domain.Enums.IndicatorMethod.Subdomain => PrimaryIndicator.Value ?? throw new InvalidDataException("primary indicator value is null"),
            _ => Tenant.ClientId
        };
    }
}
