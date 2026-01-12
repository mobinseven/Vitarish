using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Accounts.Interface.Components.Account.Widgets;

public partial class TenantsWidget : IdentityCommonBase
{
    private readonly List<TenantAccessEntry> tenantEntries = [];
    private bool isLoading;

    [Inject] private IPartiesService PartiesService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
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

            List<Guid> ownerIds = [.. tenants.Values
                .Where(tenant => tenant is not null)
                .Select(tenant => tenant!.OwnerId)
                .Distinct()];

            Dictionary<Guid, Task<Owner?>> ownerTasks = ownerIds
                .ToDictionary(id => id, id => PartiesService.GetOwnerAsync(id));

            Dictionary<Guid, Task<IEnumerable<TenantIndicator>>> indicatorTasks = tenants.Values
                .Where(tenant => tenant is not null)
                .ToDictionary(tenant => tenant!.Id, tenant => PartiesService.GetTenantIndicatorsAsync(tenant!.Id));

            _ = await Task.WhenAll(ownerTasks.Values);
            _ = await Task.WhenAll(indicatorTasks.Values);

            Dictionary<Guid, Owner?> owners = ownerTasks
                .ToDictionary(entry => entry.Key, entry => entry.Value.Result);

            foreach (IGrouping<Guid, TenantRoleAssignment> grouping in tenantRoleAssignments.GroupBy(role => role.TenantId))
            {
                Tenant? tenant = tenants.GetValueOrDefault(grouping.Key);
                Owner? owner = null;
                AuthenticationClient? client = null;
                TenantIndicator? primaryIndicator = null;

                if (tenant is not null)
                {
                    _ = owners.TryGetValue(tenant.OwnerId, out owner);

                    if (indicatorTasks.TryGetValue(tenant.Id, out Task<IEnumerable<TenantIndicator>>? indicatorsTask))
                    {
                        primaryIndicator = indicatorsTask.Result
                            .OrderByDescending(indicator => indicator.IsPrimary)
                            .FirstOrDefault();
                    }
                }

                if (primaryIndicator is null)
                {
                    continue;
                }

                List<string> roleDisplay = [.. grouping.Select(role => role.DisplayName)];

                tenantEntries.Add(new TenantAccessEntry(
                    grouping.Key,
                    tenant,
                    owner,
                    roleDisplay,
                    primaryIndicator));
            }
        }
        finally
        {
            isLoading = false;
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
        Owner? Owner,
        List<string> RoleDisplay,
        TenantIndicator? PrimaryIndicator)
    {
        public string TenantDisplayName => string.IsNullOrWhiteSpace(Tenant?.Name)
            ? $"Tenant {TenantId.ToString()[..8]}"
            : Tenant!.Name;

        public string OwnerDisplayName => string.IsNullOrWhiteSpace(Owner?.Name)
            ? "Workspace owner"
            : Owner!.Name;

        public string PrimaryIdentifier => PrimaryIndicator?.Value ?? string.Empty;
        public bool HasPrimaryIdentifier => !string.IsNullOrWhiteSpace(PrimaryIdentifier);
        public string PrimaryLabel => HasPrimaryIdentifier ? PrimaryIdentifier : string.Empty;
        public string PrimaryUrl => PrimaryIndicator.Method switch
        {
            Parties.Domain.Enums.IndicatorMethod.Subdomain => PrimaryIndicator.Value ?? throw new InvalidDataException("primary indicator value is null"),
            _ => Tenant.ClientId
        };

        public string Initials => BuildInitials(TenantDisplayName);

        private static string BuildInitials(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "?";
            }

            string[] parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 1
                ? parts[0][..1].ToUpperInvariant()
                : string.Concat(
                parts[0][..1],
                parts[1][..1]).ToUpperInvariant();
        }
    }
}
