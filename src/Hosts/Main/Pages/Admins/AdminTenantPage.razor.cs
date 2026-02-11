using Vitarish.Interfaces.Admins.Components;
using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Pages.Admins;

public partial class AdminTenantPage
{
    [Parameter] public string? TenantId { get; set; }

    private Guid tenantGuid;
    private TenantReadDto? Tenant;
    protected override async Task ReloadRequired()
    {
        if (TenantId is null)
        {
            NavigationManager.NavigateTo("/Admin/Tenants");
            return;
        }
        tenantGuid = Guid.Parse(TenantId);

        Tenant =  await PartiesService.GetTenantAsync(tenantGuid);

        if (Tenant == null)
        {
            NavigationManager.NavigateTo("/Admin/Tenants");
        }
    }

    private async Task EditTenant()
    {
        DialogParameters parameters = new()
        {
            {"Tenant",   Tenant }
        };
        var dialog = await DialogService.ShowAsync<TenantEditDialog>(null, parameters);
        var result = await dialog.Result;
        if(result is null || result.Canceled)
        {
            return;
        }

        NavigationManager.Refresh();

    }

    private async Task RemoveTenant()
    {
        await PartiesService.DeleteTenantAsync(tenantGuid);
        NavigationManager.NavigateTo("/Admin/Tenants");
    }

}
