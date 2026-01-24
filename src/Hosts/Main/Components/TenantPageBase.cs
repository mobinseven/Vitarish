using Vitarish.Interfaces.Base.Common;
using Vitarish.Modules.Base;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Users.Domain.Models;
using Metadata = Vitarish.Requests.Application.Models.Metadata;

namespace Vitarish.Main.Components;

public class TenantPageBase : CommonBase, IDisposable
{
    [Inject] protected IPartiesService PartiesService { get; set; } = default!;
    [Inject] protected ITenantService TenantService { get; set; } = default!;
    [Inject] protected IUsersService UsersService { get; set; } = default!;

    protected Tenant? Tenant;
    protected bool IsAdmin;
    protected bool IsOwner;

    protected Guid? UserId => Guid.Parse(RequestContext.UserId ?? Guid.Empty.ToString());

    protected override async Task OnInitializedAsync()
    {
        IsAdmin = RequestContext.IsTenantAdmin() || RequestContext.IsTenantOwner();
        IsOwner = RequestContext.IsTenantOwner();

        Tenant = await PartiesService.GetTenantAsync(Guid.Parse(RequestContext.TenantId));
        if (Tenant is not null)
        {
            TenantService.Metadata = new Metadata(Tenant.Metadata);
            TenantService.Metadata.OnUpdate += OnUpdatedMetadata;
            TenantService.Users = [.. (await UsersService.GetUsersInRoleAsync(RequestContext.GetTenantUserRole(), CancellationToken.None)).Select(u => new UserDto(u))];
            NavService.OverridePageTitle(Tenant.ClientId, Tenant.Name);
        }
        StateHasChanged();
    }


    private async void OnUpdatedMetadata(string key, object? value)
    {
        Tenant!.Metadata = TenantService.Metadata?.ToDictionary() ?? [];
        _ = await PartiesService.UpdateTenantAsync(new Parties.Application.Commands.UpdateTenant(Tenant.Id, Tenant.OwnerId, Tenant.Name, Tenant.ClientId, Metadata: Tenant!.Metadata));
    }

    public void Dispose()
    {
        TenantService.Metadata?.OnUpdate -= OnUpdatedMetadata;
        GC.SuppressFinalize(this);
    }
}
