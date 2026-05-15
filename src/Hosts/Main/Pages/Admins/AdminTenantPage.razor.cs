using Vitarish.Parties.Domain.Dtos;
using Vitarish.Interfaces.Base.Common;

namespace Vitarish.Main.Pages.Admins;

public partial class AdminTenantPage
{
    [Parameter] public string? TenantId { get; set; }
    [Parameter] public string? OwnerId { get; set; }

    private Guid tenantGuid;
    private TenantReadDto? Tenant;
    private void Back() => NavigationManager.NavigateTo(AppRoutes.Admin.Owner(OwnerId));
    protected override async Task ReloadRequired()
    {
        if (TenantId is null)
        {
            Back();
            return;
        }
        tenantGuid = Guid.Parse(TenantId);

        Tenant = await PartiesService.GetTenantAsync(tenantGuid);

        if (Tenant == null)
        {
            Back();
        }
    }

    private async Task RemoveTenant()
    {
        await PartiesService.DeleteTenantAsync(tenantGuid);
        Back();
    }

}
