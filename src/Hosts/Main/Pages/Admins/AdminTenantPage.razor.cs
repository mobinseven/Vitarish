using Vitarish.Parties.Domain.Dtos;
using Vitarish.Interfaces.Base.Common;

namespace Vitarish.Main.Pages.Admins;

public partial class AdminTenantPage
{
    [Parameter] public string? TenantId { get; set; }
    [Parameter] public string? OwnerId { get; set; }

    private Guid tenantGuid;
    private Guid _ownerGuid;
    private TenantReadDto? Tenant;
    private void Back() => NavigationManager.NavigateTo(
        _ownerGuid != Guid.Empty ? AppRoutes.Admin.Owner(_ownerGuid) : AppRoutes.Admin.Owners);
    protected override async Task ReloadRequired()
    {
        if (TenantId is null)
        {
            Back();
            return;
        }
        tenantGuid = Guid.Parse(TenantId);
        Guid.TryParse(OwnerId, out _ownerGuid);

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
