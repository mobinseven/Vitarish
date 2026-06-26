using Vitarish.Interfaces.Admins.Components;
using Vitarish.Parties.Client;
using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Tenants;


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

        PartiesResult<TenantReadDto> tenantResult = await PartiesClient.GetTenantAsync(tenantGuid);
        if (tenantResult.IsSuccess && tenantResult.Value is { } ten)
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
        selectedModules = new HashSet<string>(TenantMetadataValues.GetActiveModules(updated.Metadata), StringComparer.OrdinalIgnoreCase);
        selectedApps = new HashSet<string>(TenantMetadataValues.GetActiveApps(updated.Metadata), StringComparer.OrdinalIgnoreCase);
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
            TenantMetadataValues.SetActiveModules(write.Metadata, selectedModules);
            TenantMetadataValues.SetActiveApps(write.Metadata, selectedApps);

            PartiesResult<TenantReadDto> updatedResult = await PartiesClient.UpdateTenantAsync(write);
            if (!updatedResult.IsSuccess || updatedResult.Value is null)
            {
                _ = Snackbar.Add(Localizer["Error"], Severity.Error);
                return;
            }

            SetTenant(updatedResult.Value);
            _ = Snackbar.Add(Localizer["Saved"], Severity.Success);
        }
        finally
        {
            saving = false;
        }
    }

    private async Task RemoveTenant()
    {
        _ = await PartiesClient.DeleteTenantAsync(tenantGuid);
        Back();
    }

}
