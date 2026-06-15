using Vitarish.Interfaces.Admins.Components;
using Vitarish.Parties.Domain;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Parties.Domain.Mappers;

namespace Vitarish.Main.Pages.Platform;

public partial class PlatformAdminTenantPage : PlatformAdminCommonBase
{
    [Parameter] public Guid? TenantId { get; set; }
    [Parameter] public Guid? OwnerId { get; set; }

    private Guid tenantGuid;
    private Guid _ownerGuid;
    private TenantReadDto? Tenant;
    private HashSet<string> selectedModules = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> selectedApps = new(StringComparer.OrdinalIgnoreCase);
    private bool saving;
    private void Back() => NavigationManager.NavigateTo(
        _ownerGuid != Guid.Empty ? AppRoutes.PlatformAdmin.Owner(_ownerGuid) : AppRoutes.PlatformAdmin.Owners);
    protected override async Task ReloadRequired()
    {
        if (TenantId is null)
        {
            Back();
            return;
        }

        Guid id = TenantId.Value;

        tenantGuid = id;
        _ownerGuid = OwnerId ?? Guid.Empty;

        TenantReadDto? ten = await PartiesService.GetTenantAsync(tenantGuid);
        if (ten is not null)
        {
            SetTenant(ten);
        }
        else
        {
            Back();
        }
    }


    private void SetTenant(TenantReadDto updated)
    {
        Tenant = updated;
        selectedModules = new HashSet<string>(TenantMetadata.GetActiveModules(updated.Metadata), StringComparer.OrdinalIgnoreCase);
        selectedApps = new HashSet<string>(TenantMetadata.GetActiveApps(updated.Metadata), StringComparer.OrdinalIgnoreCase);
    }

    private async Task Save()
    {
        if (Tenant is null)
        {
            return;
        }

        saving = true;
        try
        {
            TenantWriteDto write = Tenant.ToWriteWithMetadata();
            TenantMetadata.SetActiveModules(write.Metadata, selectedModules);
            TenantMetadata.SetActiveApps(write.Metadata, selectedApps);

            TenantReadDto? updated = await PartiesService.UpdateTenantAsync(write);
            if (updated is null)
            {
                _ = Snackbar.Add(Localizer["Error"], Severity.Error);
                return;
            }

            SetTenant(updated);
            _ = Snackbar.Add(Localizer["Saved"], Severity.Success);
        }
        finally
        {
            saving = false;
        }
    }

    private async Task RemoveTenant()
    {
        await PartiesService.DeleteTenantAsync(tenantGuid);
        Back();
    }

}
