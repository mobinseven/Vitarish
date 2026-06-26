using Vitarish.Parties.Client;
using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Tenants;

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

        PartiesResult<IReadOnlyList<TenantReadDto>> result = await PartiesClient.GetTenantsByOwnerAsync(ownerGuid.Value);
        tenants = result.IsSuccess && result.Value is not null ? [.. result.Value] : [];
        initialized = true;
    }
}
