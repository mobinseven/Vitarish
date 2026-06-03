using Microsoft.AspNetCore.Components;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Models;

namespace Vitarish.Interfaces.Widgets.Components;

public partial class CurrentUserTenantsWidget : CommonBase
{
    private Dictionary<Guid, UserTenant> tenants = [];

    [Inject] private IPartiesService PartiesService { get; set; } = default!;
    [Parameter] public EventCallback<string> OnTenantNavigate { get; set; }

    protected override async Task ReloadRequired()
    {
        if (RequestContext.UserId is null)
        {
            return;
        }

        IEnumerable<UserTenant> userTenants = await PartiesService.GetUserTenantsAsync();
        tenants = userTenants.ToDictionary(t => t.TenantId);
    }

    private Task NavigateToTenant(Guid id)
    {
        UserTenant tenant = tenants[id];
        string url = $"https://{tenant.IndicatorValue}/{tenant.TenantAppId}";
        return OnTenantNavigate.InvokeAsync(url);
    }
}
