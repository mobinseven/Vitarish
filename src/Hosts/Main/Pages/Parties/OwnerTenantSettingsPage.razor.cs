using Vitarish.Parties.Domain;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Parties.Domain.Mappers;

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
        selectedModules = new HashSet<string>(TenantMetadata.GetActiveModules(updated.Metadata), StringComparer.OrdinalIgnoreCase);
        selectedApps = new HashSet<string>(TenantMetadata.GetActiveApps(updated.Metadata), StringComparer.OrdinalIgnoreCase);
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
}
