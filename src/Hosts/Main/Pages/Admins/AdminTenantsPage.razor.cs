using Vitarish.Admins.Interface.Components.Admin;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Main.Pages.Admins;

public partial class AdminTenantsPage
{
    private List<Tenant> Tenants { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        Tenants = [.. await PartiesService.GetTenantsAsync()];
    }

    private void RowClicked(Tenant tenant)
    {
        NavigationManager.NavigateTo($"/Admin/Tenants/{tenant.Id}");
    }

    private async Task AddTenant()
    {
        var dialog = await DialogService.ShowAsync<TenantEditDialog>();
        var result = await dialog.Result;
        if(result is null || result.Canceled)
        {
            return;
        }
    }

}
