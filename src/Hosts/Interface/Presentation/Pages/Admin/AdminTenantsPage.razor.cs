using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Interface.Presentation.Pages.Admin;

public partial class AdminTenantsPage
{
    private List<Tenant> Tenants { get; set; } = [];
    private List<Owner> Owners { get; set; } = [];
    private List<AuthenticationClient> AuthenticationClients { get; set; } = [];
    private string newTenantName = string.Empty, newTenantLogoUrl = string.Empty, newTenantDescription = string.Empty, selectedClientId = string.Empty;
    private Guid selectedOwnerId;
    private Tenant? editingTenant;

    protected override async Task OnInitializedAsync()
    {
        Tenants = [.. await PartiesService.GetTenantsAsync()];
        Owners = [.. await PartiesService.GetOwnersAsync()];
        AuthenticationClients = [.. await AuthenticationClientsService.GetAuthenticationClientsAsync()];

        if (Owners.Count > 0)
        {
            selectedOwnerId = Owners[0].Id;
        }

        if (AuthenticationClients.Count > 0)
        {
            selectedClientId = AuthenticationClients[0].Id;
        }
    }

    private void RowClicked(DataGridRowClickEventArgs<Tenant> args)
    {
        editingTenant = args.Item;
        selectedOwnerId = editingTenant.OwnerId;
        newTenantName = editingTenant.Name;
        newTenantLogoUrl = editingTenant.LogoUrl ?? string.Empty;
        newTenantDescription = editingTenant.Description ?? string.Empty;
        selectedClientId = editingTenant.ClientId ?? string.Empty;
    }

    private async Task SaveTenant()
    {
        if (string.IsNullOrWhiteSpace(selectedClientId))
        {
            _ = Snackbar.Add("Client ID is required", Severity.Error);
            return;
        }

        if (editingTenant is null)
        {
            CreateTenant request = new(
                selectedOwnerId,
                newTenantName,
                selectedClientId,
                string.IsNullOrWhiteSpace(newTenantLogoUrl) ? null : newTenantLogoUrl,
                string.IsNullOrWhiteSpace(newTenantDescription) ? null : newTenantDescription);
            Tenant? tenant = await PartiesService.CreateTenantAsync(request);
            if (tenant is not null)
            {
                Tenants.Add(tenant);
            }
        }
        else
        {
            UpdateTenant request = new(
                editingTenant.Id,
                selectedOwnerId,
                newTenantName,
                selectedClientId,
                string.IsNullOrWhiteSpace(newTenantLogoUrl) ? null : newTenantLogoUrl,
                string.IsNullOrWhiteSpace(newTenantDescription) ? null : newTenantDescription);
            Tenant? updated = await PartiesService.UpdateTenantAsync(request);
            if (updated is not null)
            {
                int idx = Tenants.FindIndex(t => t.Id == updated.Id);
                if (idx >= 0)
                {
                    Tenants[idx] = updated;
                }
            }
            editingTenant = null;
        }

        newTenantName = string.Empty;
        newTenantLogoUrl = string.Empty;
        newTenantDescription = string.Empty;
        if (AuthenticationClients.Count > 0)
        {
            selectedClientId = AuthenticationClients[0].Id;
        }
        StateHasChanged();
    }

    private async Task RemoveTenant(Tenant tenant)
    {
        await PartiesService.DeleteTenantAsync(tenant.Id);
        _ = Tenants.Remove(tenant);
        if (editingTenant?.Id == tenant.Id)
        {
            CancelEditTenant();
        }
        StateHasChanged();
    }

    private void CancelEditTenant()
    {
        editingTenant = null;
        newTenantName = string.Empty;
        newTenantLogoUrl = string.Empty;
        newTenantDescription = string.Empty;
        if (AuthenticationClients.Count > 0)
        {
            selectedClientId = AuthenticationClients[0].Id;
        }
    }
}
