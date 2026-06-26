using Vitarish.Parties.Client;
using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Tenants;


namespace Vitarish.Main.Pages.Parties;

public partial class OwnerTenantSettingsPage
{
    [Parameter] public Guid? TenantId { get; set; }

    private TenantReadDto? tenant;
    private HashSet<string> selectedModules = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> selectedApps = new(StringComparer.OrdinalIgnoreCase);
    private bool saving;

    protected override async Task ReloadRequired()
    {
        TenantReadDto? ten = await LoadTenantOrRedirectAsync(TenantId, AppRoutes.Owners.Tenants);
        if (ten is not null)
        {
            SetTenant(ten);
        }
    }

    private void SetTenant(TenantReadDto updated)
    {
        tenant = updated;
        selectedModules = new HashSet<string>(TenantMetadataValues.GetActiveModules(updated.Metadata), StringComparer.OrdinalIgnoreCase);
        selectedApps = new HashSet<string>(TenantMetadataValues.GetActiveApps(updated.Metadata), StringComparer.OrdinalIgnoreCase);
    }

    private async Task Save()
    {
        if (tenant is null)
        {
            return;
        }

        saving = true;
        try
        {
            TenantWriteDto write = tenant.ToWriteWithMetadata();
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
}
