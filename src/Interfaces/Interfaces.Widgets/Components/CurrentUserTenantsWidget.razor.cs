using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Vitarish.Interfaces.Accounts.Abstractions;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Models;

namespace Vitarish.Interfaces.Widgets.Components;

public partial class CurrentUserTenantsWidget : CommonBase
{
    private readonly Dictionary<Guid, UserTenantView> tenants = [];

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IPartiesService PartiesService { get; set; } = default!;
    [Parameter] public EventCallback<string> OnTenantNavigate { get; set; }

    protected override async Task ReloadRequired()
    {
        if (RequestContext.UserId is null)
        {
            return;
        }

        IEnumerable<IdentityRole> roles = await UserService.GetUserRolesAsync(RequestContext.UserId);
        List<Guid> tenantIds = [.. ParseTenantRoles(roles.Select(ur => ur.Name).Cast<string>()).Distinct()];

        if (tenantIds.Count == 0)
        {
            return;
        }

        // Single database call to retrieve all user tenants with their primary indicators
        IEnumerable<UserTenantView> userTenants = await PartiesService.GetUserTenantsAsync(tenantIds);

        foreach (UserTenantView tenant in userTenants)
        {
            if (tenant.IndicatorValue != null)
            {
                tenants.Add(tenant.TenantId, tenant);
            }
        }
    }

    private Task NavigateToTenant(Guid id)
    {
        UserTenantView tenant = tenants[id];
        string url = $"https://{tenant.IndicatorValue}/{tenant.TenantAppId}";
        return OnTenantNavigate.InvokeAsync(url);
    }

    private static List<Guid> ParseTenantRoles(IEnumerable<string> roles)
    {
        List<Guid> tenantIds = [];

        foreach (string role in roles)
        {
            int separatorIndex = role.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            string tenantIdText = role[..separatorIndex];
            string roleName = role[(separatorIndex + 1)..];

            if (string.IsNullOrWhiteSpace(roleName))
            {
                continue;
            }

            if (!Guid.TryParse(tenantIdText, out Guid tenantId))
            {
                continue;
            }

            tenantIds.Add(tenantId);
        }

        return tenantIds;
    }
}
