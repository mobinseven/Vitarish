using Microsoft.AspNetCore.Components;
using MudBlazor;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.ApiClients.Application.Abstractions;
using Vitarish.Accounts.Interface.Localization;

namespace Vitarish.Accounts.Interface.Components.Admin;

public partial class UserRoleAssignmentDialog : ComponentBase
{
    private readonly List<TenantRoleOption> tenantRoles = [];
    private bool isLoading;
    private TenantRoleOption? selectedRole;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public string UserName { get; set; } = string.Empty;
    [Parameter] public List<string> AvailableRoles { get; set; } = [];
    [Parameter] public List<string> AssignedRoles { get; set; } = [];

    [Inject] private IPartiesService PartiesService { get; set; } = default!;
    [Inject] private IApiClientsService ApiClientService { get; set; } = default!;
    [Inject] private IStringLocalizer Localizer { get; set; } = default!;

    private List<string> assignedRoles = [];

    protected override async Task OnInitializedAsync()
    {
        assignedRoles = [.. AssignedRoles];
        await LoadTenantRolesAsync();
    }

    private async Task LoadTenantRolesAsync()
    {
        isLoading = true;
        tenantRoles.Clear();
        StateHasChanged();

        try
        {
            IEnumerable<Tenant> tenants = await PartiesService.GetTenantsAsync();
            Dictionary<Guid, Tenant> tenantDict = tenants.ToDictionary(t => t.Id);

            List<Guid> ownerIds = tenants.Select(t => t.OwnerId).Distinct().ToList();
            Dictionary<Guid, Task<Owner?>> ownerTasks = ownerIds.ToDictionary(id => id, id => PartiesService.GetOwnerAsync(id));
            await Task.WhenAll(ownerTasks.Values);
            Dictionary<Guid, Owner?> owners = ownerTasks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Result);

            List<string> clientIds = tenants
                .Where(t => !string.IsNullOrWhiteSpace(t.ClientId))
                .Select(t => t.ClientId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            Dictionary<string, Task<ApiClient?>> clientTasks = clientIds
                .ToDictionary(id => id, id => ApiClientService.GetApiClientAsync(id), StringComparer.OrdinalIgnoreCase);
            await Task.WhenAll(clientTasks.Values);
            Dictionary<string, ApiClient?> clients = clientTasks
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Result, StringComparer.OrdinalIgnoreCase);

            foreach (string role in AvailableRoles)
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
                string clientId = client?.Id ?? (tenant?.ClientId ?? "-");

                tenantRoles.Add(new TenantRoleOption(
                    tenantId,
                    tenantName,
                    ownerName,
                    clientId,
                    roleName,
                    role));
            }

            tenantRoles.Sort((a, b) =>
            {
                int tenantCompare = string.Compare(a.TenantName, b.TenantName, StringComparison.OrdinalIgnoreCase);
                return tenantCompare != 0 ? tenantCompare : string.Compare(a.RoleName, b.RoleName, StringComparison.OrdinalIgnoreCase);
            });
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void Cancel() => MudDialog.Cancel();

    private void Assign()
    {
        if (selectedRole == null)
        {
            return;
        }

        MudDialog.Close(DialogResult.Ok(selectedRole.FullRoleName));
    }

    private sealed record TenantRoleOption(
        Guid TenantId,
        string TenantName,
        string OwnerName,
        string ClientId,
        string RoleName,
        string FullRoleName)
    {
        public string DisplayName => $"{RoleName} ({TenantName})";
    }
}
