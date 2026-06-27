using Microsoft.AspNetCore.Components;
using Vitarish.Modules.Base.Interface.Common;
using Vitarish.Parties.Client;
using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Interfaces.Aggregator.Components;

public partial class CurrentUserTenantsWidget : CommonBase
{
    private IEnumerable<UserTenant> userTenants = [];

    [Inject] private IPartiesService PartiesService { get; set; } = default!;
    [Parameter] public EventCallback<string> OnTenantNavigate { get; set; }

    protected override async Task ReloadRequired()
    {
        if (RequestContext.UserId is null)
        {
            return;
        }

         userTenants = await PartiesService.GetUserTenantsAsync();
    }

    private Task NavigateToTenant(UserTenant tenant)
    {
        string url = $"https://{tenant.IndicatorValue}/{tenant.TenantAppId}";
        return OnTenantNavigate.InvokeAsync(url);
    }
}
