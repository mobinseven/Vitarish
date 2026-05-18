using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Pages.Owners;

public partial class OwnerTenantPage
{
    [Parameter] public Guid? TenantId { get; set; }

    private TenantReadDto? tenant;

    protected override async Task ReloadRequired()
    {
        tenant = await LoadTenantOrRedirectAsync(TenantId, AppRoutes.Owners.Tenants);
        if(tenant is null)
        {
            return;
        }
        NavService.OverridePageTitle(tenant.Id.ToString(), tenant.Name);
    }
}
