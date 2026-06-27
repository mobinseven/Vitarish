using Microsoft.AspNetCore.Components;
using Vitarish.Modules.Aggregator.Interface.Common;
using Vitarish.Parties.Client;
using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Modules.Aggregator.Interface.Components;

public partial class CurrentUserTenantsWidget : CommonBase
{
    private IEnumerable<UserTenantDto> userTenants = [];

    [Inject] private IPartiesClient PartiesService { get; set; } = default!;
    [Parameter] public EventCallback<string> OnTenantNavigate { get; set; }

    protected override async Task ReloadRequired()
    {
        userTenants = (await PartiesService.GetUserTenantsAsync()).Value ?? [];
    }

    private Task NavigateToTenant(UserTenantDto tenant)
    {
        string url = $"https://{tenant.PrimaryIndicatorValue}/{tenant.AppId}";
        return OnTenantNavigate.InvokeAsync(url);
    }
}
