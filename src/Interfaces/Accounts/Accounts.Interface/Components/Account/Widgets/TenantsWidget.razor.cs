using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Accounts.Interface.Components.Account.Widgets;

public partial class TenantsWidget : CommonBase
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
            ClaimsPrincipal claimsPrincipal = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
            if (!(claimsPrincipal.Identity?.IsAuthenticated ?? false))
            {
                _ = Snackbar.Add(Localizer["InvalidLoginAttempt"], Severity.Warning);
                return;
            }

            string? userId = claimsPrincipal.FindFirstValue("sub")
                ?? claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                _ = Snackbar.Add(Localizer["Error"], Severity.Error);
                return;
            }

            var roles = await UserService.GetUserRolesAsync(User.Id);
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

            List<string> clientIds = [.. tenants.Values
                .Where(tenant => tenant is not null && !string.IsNullOrWhiteSpace(tenant.ClientId))
                .Select(tenant => tenant!.ClientId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)];

            Dictionary<string, Task<ApiClient?>> clientTasks = clientIds
                .ToDictionary(id => id, id => ApiClientService.GetApiClientAsync(id), StringComparer.OrdinalIgnoreCase);

            Dictionary<Guid, Task<IEnumerable<TenantIndicator>>> indicatorTasks = tenants.Values
                .Where(tenant => tenant is not null)
                .ToDictionary(tenant => tenant!.Id, tenant => PartiesService.GetTenantIndicatorsAsync(tenant!.Id));

            _ = await Task.WhenAll(ownerTasks.Values);
            _ = await Task.WhenAll(clientTasks.Values);
            _ = await Task.WhenAll(indicatorTasks.Values);

            Dictionary<Guid, Owner?> owners = ownerTasks
                .ToDictionary(entry => entry.Key, entry => entry.Value.Result);
            Dictionary<string, ApiClient?> clients = clientTasks
                .ToDictionary(entry => entry.Key, entry => entry.Value.Result, StringComparer.OrdinalIgnoreCase);

            foreach (IGrouping<Guid, TenantRoleAssignment> grouping in tenantRoleAssignments.GroupBy(role => role.TenantId))
            {
                Tenant? tenant = tenants.GetValueOrDefault(grouping.Key);
                Owner? owner = null;
                ApiClient? client = null;
                TenantIndicator? primaryIndicator = null;

                if (tenant is not null)
                {
                    _ = owners.TryGetValue(tenant.OwnerId, out owner);

                    if (!string.IsNullOrWhiteSpace(tenant.ClientId))
                    {
                        _ = clients.TryGetValue(tenant.ClientId, out client);
                    }

                    if (indicatorTasks.TryGetValue(tenant.Id, out Task<IEnumerable<TenantIndicator>>? indicatorsTask))
                    {
                        primaryIndicator = indicatorsTask.Result
                            .OrderByDescending(indicator => indicator.IsPrimary)
                            .FirstOrDefault();
                    }
                }

                List<string> roleDisplay = [.. grouping.Select(role => role.DisplayName)];

                tenantEntries.Add(new TenantAccessEntry(
                    grouping.Key,
                    tenant,
                    owner,
                    client,
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

        NavigationManager.NavigateTo(BuildTenantUrl(entry.PrimaryIdentifier), forceLoad: true);
    }

    private static string BuildTenantUrl(string identifier) => Uri.TryCreate(identifier, UriKind.Absolute, out Uri? absolute) ? absolute.ToString() : $"https://{identifier}";

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
        ApiClient? Client,
        List<string> RoleDisplay,
        TenantIndicator? PrimaryIndicator)
    {
        public string TenantDisplayName => string.IsNullOrWhiteSpace(Tenant?.Name)
            ? $"Tenant {TenantId.ToString()[..8]}"
            : Tenant!.Name;

        public string OwnerDisplayName => string.IsNullOrWhiteSpace(Owner?.Name)
            ? "Workspace owner"
            : Owner!.Name;

        public string ClientDisplay => string.IsNullOrWhiteSpace(Client?.HostUri)
            ? "Client workspace"
            : Client!.HostUri;

        public string PrimaryIdentifier => PrimaryIndicator?.Value ?? string.Empty;
        public bool HasPrimaryIdentifier => !string.IsNullOrWhiteSpace(PrimaryIdentifier);
        public string PrimaryLabel => HasPrimaryIdentifier ? PrimaryIdentifier : string.Empty;
        public string PrimaryUrl => HasPrimaryIdentifier ? BuildTenantUrl(PrimaryIdentifier) : string.Empty;

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
