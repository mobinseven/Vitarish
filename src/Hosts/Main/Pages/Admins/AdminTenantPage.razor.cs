using Vitarish.Parties.Domain;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Interfaces.Base.Common;

namespace Vitarish.Main.Pages.Admins;

public partial class AdminTenantPage
{
    [Parameter] public Guid? TenantId { get; set; }
    [Parameter] public Guid? OwnerId { get; set; }

    private Guid tenantGuid;
    private Guid _ownerGuid;
    private TenantReadDto? Tenant;
    private HashSet<string> selectedModules = new(StringComparer.OrdinalIgnoreCase);
    private bool savingModules;
    private void Back() => NavigationManager.NavigateTo(
        _ownerGuid != Guid.Empty ? AppRoutes.Admin.Owner(_ownerGuid) : AppRoutes.Admin.Owners);
    protected override async Task ReloadRequired()
    {
        if (TenantId is not { } id)
        {
            Back();
            return;
        }

        tenantGuid = id;
        _ownerGuid = OwnerId ?? Guid.Empty;

        Tenant = await PartiesService.GetTenantAsync(tenantGuid);
        selectedModules = new HashSet<string>(TenantMetadata.GetActiveModules(Tenant?.Metadata), StringComparer.OrdinalIgnoreCase);

        if (Tenant == null)
        {
            Back();
        }
    }

    private void SetTenant(TenantReadDto tenant)
    {
        Tenant = tenant;
        selectedModules = new HashSet<string>(TenantMetadata.GetActiveModules(tenant.Metadata), StringComparer.OrdinalIgnoreCase);
    }

    private async Task SaveTenantModules()
    {
        if (Tenant is null)
        {
            return;
        }

        savingModules = true;
        try
        {
            TenantWriteDto write = Tenant.ToWriteWithMetadata();
            TenantMetadata.SetActiveModules(write.Metadata, selectedModules);

            TenantReadDto? updated = await PartiesService.UpdateTenantAsync(write);
            if (updated is null)
            {
                Snackbar.Add(Localizer["Error"], Severity.Error);
                return;
            }

            SetTenant(updated);
            Snackbar.Add(Localizer["Saved"], Severity.Success);
        }
        finally
        {
            savingModules = false;
        }
    }

    private async Task RemoveTenant()
    {
        await PartiesService.DeleteTenantAsync(tenantGuid);
        Back();
    }

}
