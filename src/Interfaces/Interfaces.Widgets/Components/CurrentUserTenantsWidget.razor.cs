using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Vitarish.Interfaces.Accounts.Abstractions;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Interfaces.Widgets.Components;

public partial class CurrentUserTenantsWidget : CommonBase
{
    private readonly Dictionary<Guid, TenantReadDto> tenants = [];
    private readonly Dictionary<Guid, TenantIndicatorReadDto> tenantsIndicators = [];

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IPartiesService PartiesService { get; set; } = default!;

    protected override async Task ReloadRequired()
    {
        if (RequestContext.UserId is null)
        {
            return;
        }

        IEnumerable<IdentityRole> roles = await UserService.GetUserRolesAsync(RequestContext.UserId);
        List<Guid> tenantIds = [.. ParseTenantRoles(roles.Select(ur => ur.Name).Cast<string>()).Distinct()];

        foreach (Guid id in tenantIds)
        {
            var tenant = await PartiesService.GetTenantAsync(id);
            if (tenant == null)
            {
                continue;
            }
            var tenantInds = await PartiesService.GetTenantIndicatorsByTenantAsync(id);
            var ind = tenantInds.FirstOrDefault();
            if (ind == null)
            {
                continue;
            }

            tenants.Add(id, tenant);
            tenantsIndicators.Add(id, ind);
        }
    }

    private void NavigateToTenant(Guid id)
    {
        string url = $"https://{tenantsIndicators[id].Value}/{tenants[id].ClientId}";
        NavigationManager.NavigateTo(url, forceLoad: true);
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
