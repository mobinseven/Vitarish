using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Pages.Parties;

public partial class OwnerDashboardPage
{
    private List<TenantReadDto> tenants = [];
    private bool initialized;

    protected override async Task ReloadRequired()
    {
        Guid? ownerGuid = GetOwnerGuid();
        if (ownerGuid is null)
        {
            initialized = true;
            return;
        }

        tenants = [.. await PartiesService.GetTenantsByOwnerAsync(ownerGuid.Value)];
        initialized = true;
    }
}
