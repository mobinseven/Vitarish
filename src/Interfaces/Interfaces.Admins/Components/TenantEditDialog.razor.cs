using Vitarish.Estates.Aggregator;
using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Interfaces.Admins.Components;

public partial class TenantEditDialog
{
    [Inject] protected IEstatesProvider EstatesProvider { get; set; } = default!;
    [Parameter] public TenantReadDto? Tenant { get; set; }
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    private string newTenantName = string.Empty, newTenantLogoUrl = string.Empty, newTenantDescription = string.Empty, selectedEstateId = string.Empty;
    private Guid selectedOwnerId;
    private List<OwnerReadDto> Owners { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        Owners = [.. await PartiesService.GetOwnersAsync()];

        if (Tenant is not null)
        {
            selectedOwnerId = Tenant.OwnerId;
            newTenantName = Tenant.Name;
            newTenantLogoUrl = Tenant.LogoUrl ?? string.Empty;
            newTenantDescription = Tenant.Description ?? string.Empty;
            selectedEstateId = Tenant.EstateId ?? string.Empty;
        }
    }

    private async Task UpdateRoles()
    {
        if (Tenant is not null)
        {
            await PartiesService.CreateTenantRolesAsync(Tenant.Id);
        }
    }

    private async Task SaveTenant()
    {
        if (string.IsNullOrWhiteSpace(selectedEstateId))
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
                EstateId = selectedEstateId,
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
                EstateId = selectedEstateId,
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
    private void Cancel() => MudDialog.Cancel();
}
