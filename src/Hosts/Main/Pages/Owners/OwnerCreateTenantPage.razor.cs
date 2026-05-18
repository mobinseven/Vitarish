using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Pages.Owners;

public partial class OwnerCreateTenantPage
{
    private void OnTenantCreated(TenantReadDto tenant) =>
        NavigationManager.NavigateTo(AppRoutes.Owners.Tenant(tenant.Id));
}
