using Microsoft.AspNetCore.Components;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Models;

namespace Vitarish.Interfaces.Widgets.Components;

public partial class CurrentUserTenantsWidget : CommonBase
{
    private readonly Dictionary<Guid, UserTenant> tenants = [];

    [Inject] private IPartiesService PartiesService { get; set; } = default!;
    [Parameter] public EventCallback<string> OnTenantNavigate { get; set; }

    protected override async Task ReloadRequired()
    {
        if (RequestContext.UserId is null)
        {
            return;
        }

        // Single database call to retrieve all user tenants with their primary indicators
        // The cross-schema view uses current_setting('app.user_id', true) to filter by the current user's roles
        IEnumerable<UserTenant> userTenants = await PartiesService.GetUserTenantsAsync();

        foreach (UserTenant tenant in userTenants)
        {
            if (tenant.IndicatorValue != null)
            {
                tenants.Add(tenant.TenantId, tenant);
            }
        }
    }

    private Task NavigateToTenant(Guid id)
    {
        UserTenant tenant = tenants[id];
        string url = $"https://{tenant.IndicatorValue}/{tenant.TenantAppId}";
        return OnTenantNavigate.InvokeAsync(url);
    }
}
