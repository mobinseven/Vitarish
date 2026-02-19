using Vitarish.Interfaces.Admins.Components;
using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Pages.Admins;

public partial class AdminTenantsPage
{
    private List<TenantReadDto> Tenants { get; set; } = [];

    protected override async Task OnInitializedAsync() => Tenants = [.. await PartiesService.GetTenantsAsync()];

    private void RowClicked(TenantReadDto tenant) => NavigationManager.NavigateTo($"/Admin/Tenants/{tenant.Id}");

    private async Task AddTenant()
    {
        IDialogReference dialog = await DialogService.ShowAsync<TenantEditDialog>();
        DialogResult? result = await dialog.Result;
        if(result is null || result.Canceled)
        {
            return;
        }
    }

}
