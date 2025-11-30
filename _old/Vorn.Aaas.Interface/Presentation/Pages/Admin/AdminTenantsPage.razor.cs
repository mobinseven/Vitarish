namespace Vorn.Aaas.Interface.Presentation.Pages.Admin;

using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Presentation.Abstractions;
using Vorn.Aaas.Parties.Presentation.Contracts;

public partial class AdminTenantsPage
{
    List<Tenant> Tenants { get; set; } = [];
    List<Owner> Owners { get; set; } = [];
    List<AuthenticationClient> AuthenticationClients { get; set; } = [];
    string newTenantName = string.Empty, newTenantLogoUrl = string.Empty, newTenantDescription = string.Empty, selectedClientId = string.Empty;
    Guid selectedOwnerId;
    Tenant? editingTenant;

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

    void RowClicked(DataGridRowClickEventArgs<Tenant> args)
    {
        editingTenant = args.Item;
        selectedOwnerId = editingTenant.OwnerId;
        newTenantName = editingTenant.Name;
        newTenantLogoUrl = editingTenant.LogoUrl ?? string.Empty;
        newTenantDescription = editingTenant.Description ?? string.Empty;
        selectedClientId = editingTenant.ClientId ?? string.Empty;
    }

    async Task SaveTenant()
    {
        if (string.IsNullOrWhiteSpace(selectedClientId))
        {
            Snackbar.Add("Client ID is required", Severity.Error);
            return;
        }

        if (editingTenant is null)
        {
            var request = new CreateTenantRequest(
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
            var request = new UpdateTenantRequest(
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

    async Task RemoveTenant(Tenant tenant)
    {
        await PartiesService.DeleteTenantAsync(tenant.Id);
        Tenants.Remove(tenant);
        if (editingTenant?.Id == tenant.Id)
        {
            CancelEditTenant();
        }
        StateHasChanged();
    }

    void CancelEditTenant()
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
