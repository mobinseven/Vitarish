using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Main.Pages.Parties;

public partial class OwnerCreateTenantPage
{
    private void OnTenantCreated(TenantReadDto tenant) =>
        NavigationManager.NavigateTo(AppRoutes.Owners.Tenant(tenant.Id));
}
