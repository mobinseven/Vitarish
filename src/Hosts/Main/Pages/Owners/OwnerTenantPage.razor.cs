using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Pages.Owners;

public partial class OwnerTenantPage
{
    [Parameter] public Guid? TenantId { get; set; }

    private TenantReadDto? tenant;

    protected override async Task ReloadRequired()
    {
        if (TenantId is null)
        {
            NavigationManager.NavigateTo(AppRoutes.Owners.Tenants);
            return;
        }

        tenant = await PartiesService.GetTenantAsync(TenantId.Value);
        if (tenant is null)
            NavigationManager.NavigateTo(AppRoutes.Owners.Tenants);
    }
}
