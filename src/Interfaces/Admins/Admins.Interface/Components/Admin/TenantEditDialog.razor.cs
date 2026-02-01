using Vitarish.Admins.Interface.Services;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Admins.Interface.Components.Admin;

public partial class TenantEditDialog
{
    [Inject] protected IPackageProvider PackageProvider { get; set; } = default!;
    [Parameter] public Tenant? Tenant { get; set; }
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    private string newTenantName = string.Empty, newTenantLogoUrl = string.Empty, newTenantDescription = string.Empty, selectedPackageId = string.Empty;
    private Guid selectedOwnerId;
    private List<Owner> Owners { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        Owners = [.. await PartiesService.GetOwnersAsync()];

        if(Tenant is not null)
        {
            selectedOwnerId = Tenant.OwnerId;
            newTenantName = Tenant.Name;
            newTenantLogoUrl = Tenant.LogoUrl ?? string.Empty;
            newTenantDescription = Tenant.Description ?? string.Empty;
            selectedPackageId = Tenant.ClientId ?? string.Empty;
        }
    }

    private async Task SaveTenant()
    {
        if (string.IsNullOrWhiteSpace(selectedPackageId))
        {
            _ = Snackbar.Add("Client ID is required", Severity.Error);
            return;
        }

        if (Tenant is null)
        {
            CreateTenant request = new(
                selectedOwnerId,
                newTenantName,
                selectedPackageId,
                string.IsNullOrWhiteSpace(newTenantLogoUrl) ? null : newTenantLogoUrl,
                string.IsNullOrWhiteSpace(newTenantDescription) ? null : newTenantDescription);
            Tenant? tenant = await PartiesService.CreateTenantAsync(request);
            if (tenant is not null)
            {
                MudDialog.Close(tenant);
            }
        }
        else
        {
            UpdateTenant request = new(
                Tenant.Id,
                selectedOwnerId,
                newTenantName,
                selectedPackageId,
                string.IsNullOrWhiteSpace(newTenantLogoUrl) ? null : newTenantLogoUrl,
                string.IsNullOrWhiteSpace(newTenantDescription) ? null : newTenantDescription);
            Tenant? updated = await PartiesService.UpdateTenantAsync(request);
            if (updated is not null)
            {
                MudDialog.Close(updated);
            }
        }

        newTenantName = string.Empty;
        newTenantLogoUrl = string.Empty;
        newTenantDescription = string.Empty;
        StateHasChanged();
    }
    private void Cancel()
    {
        MudDialog.Cancel();
    }
}
