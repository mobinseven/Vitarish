using Vitarish.Parties.Client;
using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Main.Pages.Parties;

public partial class OwnerTenantsPage
{
    private List<TenantReadDto> tenants = [];

    protected override async Task ReloadRequired()
    {
        Guid? ownerGuid = GetOwnerGuid();
        if (ownerGuid is null)
        {
            return;
        }

        PartiesResult<IReadOnlyList<TenantReadDto>> result = await PartiesClient.GetTenantsByOwnerAsync(ownerGuid.Value);
        tenants = result.IsSuccess && result.Value is not null ? [.. result.Value] : [];
    }
}
