
using Vitarish.Interfaces.Base.Common;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Main.Components;

public class TenantPageBase : CommonBase
{
    [Inject] protected IRequestContext RequestContext { get; set; } = default!;
    [Inject] protected IPartiesService PartiesService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthStateTask { get; set; } = default!;
    protected Tenant Tenant;
    protected bool IsAdmin;
    protected bool IsOwner;
    protected Guid? userId;
    protected sealed override async Task OnInitializedAsync()
    {
        ClaimsPrincipal user = (await AuthStateTask).User;
        IsAdmin = user.Identity?.IsAuthenticated == true && user.IsInRole($"{RequestContext.TenantId}:Admin");
        IsOwner = user.Identity?.IsAuthenticated == true && user.IsInRole($"{RequestContext.TenantId}:Owner");
        userId = Guid.Parse(user.GetUserId());

        Tenant = await PartiesService.GetTenantAsync(Guid.Parse(RequestContext.TenantId));

        NavService.OverridePageTitle(Tenant.ClientId, Tenant.Name);
    }
}
