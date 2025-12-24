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

            Dictionary<Guid, Owner?> owners = [];
            Dictionary<string, ApiClient?> clients = new(StringComparer.OrdinalIgnoreCase);

            foreach (IGrouping<Guid, TenantRoleAssignment> grouping in tenantRoleAssignments.GroupBy(role => role.TenantId))
            {
                Tenant? tenant = await PartiesService.GetTenantAsync(grouping.Key);
                Owner? owner = null;
                ApiClient? client = null;
                TenantIndicator? primaryIndicator = null;

                if (tenant is not null)
                {
                    if (!owners.TryGetValue(tenant.OwnerId, out owner))
                    {
                        owner = await PartiesService.GetOwnerAsync(tenant.OwnerId);
                        owners[tenant.OwnerId] = owner;
                    }

                    if (!string.IsNullOrWhiteSpace(tenant.ClientId)
                        && !clients.TryGetValue(tenant.ClientId, out client))
                    {
                        client = await ApiClientService.GetApiClientAsync(tenant.ClientId);
                        clients[tenant.ClientId] = client;
                    }

                    IEnumerable<TenantIndicator> indicators = await PartiesService.GetTenantIndicatorsAsync(tenant.Id);
                    primaryIndicator = indicators
                        .OrderByDescending(indicator => indicator.IsPrimary)
                        .FirstOrDefault();
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
