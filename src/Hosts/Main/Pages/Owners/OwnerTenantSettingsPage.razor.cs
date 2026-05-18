using Vitarish.Parties.Domain;
using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Pages.Owners;

public partial class OwnerTenantSettingsPage
{
    [Parameter] public Guid? TenantId { get; set; }

    private TenantReadDto? tenant;
    private HashSet<string> selectedModules = new(StringComparer.OrdinalIgnoreCase);
    private bool savingModules;

    protected override async Task ReloadRequired()
    {
        tenant = await LoadTenantOrRedirectAsync(TenantId, AppRoutes.Owners.Tenants);
        if (tenant is not null)
            selectedModules = new HashSet<string>(TenantMetadata.GetActiveModules(tenant.Metadata), StringComparer.OrdinalIgnoreCase);
    }

    private void SetTenant(TenantReadDto updated)
    {
        tenant = updated;
        selectedModules = new HashSet<string>(TenantMetadata.GetActiveModules(updated.Metadata), StringComparer.OrdinalIgnoreCase);
    }

    private async Task SaveModules()
    {
        if (tenant is null)
            return;

        savingModules = true;
        try
        {
            TenantWriteDto write = tenant.ToWriteWithMetadata();
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
}
