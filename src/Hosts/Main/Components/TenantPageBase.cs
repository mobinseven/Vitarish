
using Microsoft.AspNetCore.Authorization;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Users.Application;

namespace Vitarish.Main.Components;

public class TenantPageBase : CommonBase
{
    [Inject] protected IRequestContext RequestContext { get; set; } = default!;
    [Inject] protected IPartiesService PartiesService { get; set; } = default!;
    [Inject] protected IAuthorizationService AuthorizationService { get; set; } = default!;

    [CascadingParameter] private Task<AuthenticationState> AuthStateTask { get; set; } = default!;

    protected Tenant? Tenant;
    protected bool IsAdmin;
    protected bool IsOwner;
    protected Guid? userId;
    protected sealed override async Task OnInitializedAsync()
    {
        ClaimsPrincipal user = (await AuthStateTask).User;

        var adminResult = await AuthorizationService.AuthorizeAsync(user, IdentityAccessConstants.Admin);
        var ownerResult = await AuthorizationService.AuthorizeAsync(user, IdentityAccessConstants.Owner);
        IsAdmin = ownerResult.Succeeded || adminResult.Succeeded;
        IsOwner = ownerResult.Succeeded;
        userId = Guid.Parse(user.GetUserId());

        Tenant = await PartiesService.GetTenantAsync(Guid.Parse(RequestContext.TenantId));
        if(Tenant is not null)
        {
            NavService.OverridePageTitle(Tenant.ClientId, Tenant.Name);
        }
    }
}
