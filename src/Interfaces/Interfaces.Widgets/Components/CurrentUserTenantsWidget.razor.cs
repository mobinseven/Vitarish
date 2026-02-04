using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Vitarish.Interfaces.Accounts.Abstractions;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Dtos;

namespace Vitarish.Interfaces.Widgets.Components;

public partial class CurrentUserTenantsWidget : CommonBase
{
    private readonly List<TenantReadDto> tenants = [];

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IPartiesService PartiesService { get; set; } = default!;

    protected override async Task ReloadRequired()
    {
        if (RequestContext.UserId is null)
        {
            return;
        }

        IEnumerable<IdentityRole> roles = await UserService.GetUserRolesAsync(RequestContext.UserId);
        List<Guid> tenantIds = ParseTenantRoles(roles.Select(ur => ur.Name).Cast<string>());

        foreach (Guid id in tenantIds)
        {
            TenantReadDto? tenant = await PartiesService.GetTenantAsync(id);
            if (tenant == null)
            {
                continue;
            }
            tenants.Add(tenant);
        }
    }

    private void NavigateToTenant(TenantReadDto entry) => NavigationManager.NavigateTo(entry.ClientId, forceLoad: true);

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
