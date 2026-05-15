using Vitarish.Modules.Base;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Users.Domain.Models;
using Metadata = Vitarish.Entities.Server.Models.Metadata;
using Vitarish.Interfaces.Base.Common;

namespace Vitarish.Main.Components;

public class TenantPageBase : PageBase, IDisposable
{
    [Inject] protected IPartiesService PartiesService { get; set; } = default!;
    [Inject] protected ITenantService TenantService { get; set; } = default!;
    [Inject] protected IUsersService UsersService { get; set; } = default!;

    protected TenantReadDto? Tenant;
    protected bool IsUser;
    protected bool IsAdmin;
    protected bool IsOwner;

    protected Guid? UserId => Guid.Parse(RequestContext.UserId ?? Guid.Empty.ToString());

    protected sealed override async Task OnInitializedAsync()
    {

        if (RequestContext.TenantId is null)
        {
            NavigationManager.NavigateTo(AppRoutes.Home);
            return;
        }

        Tenant = await PartiesService.GetTenantAsync(Guid.Parse(RequestContext.TenantId));

        if (Tenant is null)
        {
            NavigationManager.NavigateTo(AppRoutes.Home);
            return;
        }

        IsUser = RequestContext.IsTenantUser();

        if (!IsUser)
        {
            NavigationManager.NavigateTo(AppRoutes.Home);
            return;
        }

        TenantService.Metadata = new Metadata(Tenant.Metadata);
        TenantService.Metadata.OnUpdate += OnUpdatedMetadata;
        TenantService.Users = [.. (await UsersService.GetUsersInRoleAsync(RequestContext.GetTenantUserRole(), CancellationToken.None)).Select(u => new UserDto(u))];

        NavService.OverridePageTitle(Tenant.AppId, Tenant.Name);

        IsAdmin = RequestContext.IsTenantAdmin() || RequestContext.IsTenantOwner();
        IsOwner = RequestContext.IsTenantOwner();

        await base.OnInitializedAsync();

        StateHasChanged();
    }


    private async void OnUpdatedMetadata(string key, object? value)
    {
        Dictionary<string, object?> metadata = TenantService.Metadata?.ToDictionary() ?? [];
        TenantWriteDto dto = new()
        {
            Id = Tenant!.Id,
            OwnerId = Tenant.OwnerId,
            Name = Tenant.Name,
            AppId = Tenant.AppId,
            LogoUrl = Tenant.LogoUrl,
            Description = Tenant.Description,
            Metadata = metadata
        };
        TenantReadDto? updated = await PartiesService.UpdateTenantAsync(dto);
        if (updated is not null)
        {
            Tenant = updated;
        }
    }

    public void Dispose()
    {
        TenantService.Metadata?.OnUpdate -= OnUpdatedMetadata;
        GC.SuppressFinalize(this);
    }
}
