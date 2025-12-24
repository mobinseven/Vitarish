using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MudBlazor;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Accounts.Interface.Components.Account;

public partial class TenantAccessList : CommonBase
{
    private readonly List<TenantAccessEntry> tenantEntries = [];
    private bool isLoading;

    [Inject] private IPartiesService PartiesService { get; set; } = default!;
    [Inject] private UserManager<User> UserManager { get; set; } = default!;

    protected override async Task OnInitializedAsync()
        => await LoadAsync();

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

            User? user = await UserManager.FindByIdAsync(userId);
            if (user is null)
            {
                _ = Snackbar.Add(Localizer["Error"], Severity.Error);
                return;
            }

            IList<string> roles = await UserManager.GetRolesAsync(user);
            List<TenantRoleAssignment> tenantRoleAssignments = ParseTenantRoles(roles);

            if (tenantRoleAssignments.Count == 0)
            {
                return;
            }

            List<Guid> tenantIds = tenantRoleAssignments
                .Select(role => role.TenantId)
                .Distinct()
                .ToList();

            Dictionary<Guid, Task<Tenant?>> tenantTasks = tenantIds
                .ToDictionary(id => id, id => PartiesService.GetTenantAsync(id));
            await Task.WhenAll(tenantTasks.Values);

            Dictionary<Guid, Tenant?> tenants = tenantTasks
                .ToDictionary(entry => entry.Key, entry => entry.Value.Result);

            List<Guid> ownerIds = tenants.Values
                .Where(tenant => tenant is not null)
                .Select(tenant => tenant!.OwnerId)
                .Distinct()
                .ToList();

            Dictionary<Guid, Task<Owner?>> ownerTasks = ownerIds
                .ToDictionary(id => id, id => PartiesService.GetOwnerAsync(id));

            List<string> clientIds = tenants.Values
                .Where(tenant => tenant is not null && !string.IsNullOrWhiteSpace(tenant.ClientId))
                .Select(tenant => tenant!.ClientId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Dictionary<string, Task<ApiClient?>> clientTasks = clientIds
                .ToDictionary(id => id, id => ApiClientService.GetApiClientAsync(id), StringComparer.OrdinalIgnoreCase);

            Dictionary<Guid, Task<IEnumerable<TenantIndicator>>> indicatorTasks = tenants.Values
                .Where(tenant => tenant is not null)
                .ToDictionary(tenant => tenant!.Id, tenant => PartiesService.GetTenantIndicatorsAsync(tenant!.Id));

            await Task.WhenAll(ownerTasks.Values);
            await Task.WhenAll(clientTasks.Values);
            await Task.WhenAll(indicatorTasks.Values);

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

    private Task ReloadAsync() => LoadAsync();

    private void NavigateToTenant(TenantAccessEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.PrimaryIdentifier))
        {
            return;
        }

        NavigationManager.NavigateTo(BuildTenantUrl(entry.PrimaryIdentifier), forceLoad: true);
    }

    private static string BuildTenantUrl(string identifier)
    {
        if (Uri.TryCreate(identifier, UriKind.Absolute, out Uri? absolute))
        {
            return absolute.ToString();
        }

        return $"https://{identifier}";
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
        ApiClient? Client,
        List<string> RoleDisplay,
        TenantIndicator? PrimaryIndicator)
    {
        public string TenantName => string.IsNullOrWhiteSpace(Tenant?.Name) ? TenantId.ToString() : Tenant.Name;
        public string OwnerName => string.IsNullOrWhiteSpace(Owner?.Name) ? Tenant?.OwnerId.ToString() ?? "-" : Owner.Name;
        public string OwnerId => Owner?.Id.ToString() ?? Tenant?.OwnerId.ToString() ?? "-";
        public string ClientId => Client?.Id ?? Tenant?.ClientId ?? "-";
        public string ClientHost => Client?.HostUri ?? "-";
        public string PrimaryIdentifier => PrimaryIndicator?.Value ?? "-";
    }
}
