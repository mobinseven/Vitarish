using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Pages.Owners;

public partial class OwnerTenantsPage
{
    private List<TenantReadDto> tenants = [];

    protected override async Task ReloadRequired()
    {
        Guid? ownerGuid = GetOwnerGuid();
        if (ownerGuid is null)
            return;

        tenants = [.. await PartiesService.GetTenantsByOwnerAsync(ownerGuid.Value)];
    }
}
