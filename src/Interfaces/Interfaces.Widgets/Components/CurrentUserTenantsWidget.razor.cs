using Microsoft.AspNetCore.Components;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Models;

namespace Vitarish.Interfaces.Widgets.Components;

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
