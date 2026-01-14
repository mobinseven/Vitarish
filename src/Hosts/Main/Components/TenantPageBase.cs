
using Microsoft.AspNetCore.Authorization;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Main.Components;

public class TenantPageBase : CommonBase
{
    [Inject] protected IRequestContext RequestContext { get; set; } = default!;
    [Inject] protected IPartiesService PartiesService { get; set; } = default!;
    [Inject] protected IAuthorizationService AuthorizationService { get; set; } = default!;


    protected Tenant? Tenant;
    protected bool IsAdmin;
    protected bool IsOwner;
    protected Guid? userId => Guid.Parse(RequestContext.UserId ?? Guid.Empty.ToString());
    protected sealed override async Task OnInitializedAsync()
    {
        IsAdmin = RequestContext.IsTenantAdmin() || RequestContext.IsTenantOwner();
        IsOwner = RequestContext.IsTenantOwner();

        Tenant = await PartiesService.GetTenantAsync(Guid.Parse(RequestContext.TenantId));
        if (Tenant is not null)
        {
            NavService.OverridePageTitle(Tenant.ClientId, Tenant.Name);
        }
    }
}
