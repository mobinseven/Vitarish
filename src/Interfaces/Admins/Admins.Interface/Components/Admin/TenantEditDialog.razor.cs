using Vitarish.Admins.Interface.Services;
using Vitarish.Parties.Application.Dtos;

namespace Vitarish.Admins.Interface.Components.Admin;

public partial class TenantEditDialog
{
    [Inject] protected IPackageProvider PackageProvider { get; set; } = default!;
    [Parameter] public TenantReadDto? Tenant { get; set; }
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    private string newTenantName = string.Empty, newTenantLogoUrl = string.Empty, newTenantDescription = string.Empty, selectedPackageId = string.Empty;
    private Guid selectedOwnerId;
    private List<OwnerReadDto> Owners { get; set; } = [];

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
            TenantWriteDto dto = new()
            {
                Id = Guid.NewGuid(),
                OwnerId = selectedOwnerId,
                Name = newTenantName,
                ClientId = selectedPackageId,
                LogoUrl = string.IsNullOrWhiteSpace(newTenantLogoUrl) ? null : newTenantLogoUrl,
                Description = string.IsNullOrWhiteSpace(newTenantDescription) ? null : newTenantDescription
            };
            TenantReadDto? tenant = await PartiesService.CreateTenantAsync(dto);
            if (tenant is not null)
            {
                MudDialog.Close(tenant);
            }
        }
        else
        {
            TenantWriteDto dto = new()
            {
                Id = Tenant.Id,
                OwnerId = selectedOwnerId,
                Name = newTenantName,
                ClientId = selectedPackageId,
                LogoUrl = string.IsNullOrWhiteSpace(newTenantLogoUrl) ? null : newTenantLogoUrl,
                Description = string.IsNullOrWhiteSpace(newTenantDescription) ? null : newTenantDescription,
                Metadata = Tenant.Metadata
            };
            TenantReadDto? updated = await PartiesService.UpdateTenantAsync(dto);
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
