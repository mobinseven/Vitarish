using Vitarish.Modules.Base;
using Vitarish.Parties.Client;
using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Tenants;
using Vitarish.Users.Domain.Models;
using Metadata = Vitarish.Entities.Contracts.Metadata;

namespace Vitarish.Main.Components;

public class TenantPageBase : PageBase, IDisposable
{
    [Inject] protected IPartiesClient PartiesClient { get; set; } = default!;
    [Inject] protected ITenantService TenantService { get; set; } = default!;
    [Inject] protected IUsersService UsersService { get; set; } = default!;

    protected TenantReadDto? Tenant;
    protected bool IsUser;
    protected bool IsOwner;

    protected Guid? UserId => Guid.Parse(RequestContext.UserId ?? Guid.Empty.ToString());

    protected sealed override async Task OnInitializedAsync()
    {

        if (RequestContext.TenantId is null)
        {
            NavigationManager.NavigateTo(AppRoutes.Home);
            return;
        }

        PartiesResult<TenantReadDto> tenantResult = await PartiesClient.GetTenantAsync(Guid.Parse(RequestContext.TenantId));
        Tenant = tenantResult.Value;

        if (!tenantResult.IsSuccess || Tenant is null)
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
        PartiesResult<TenantReadDto> updated = await PartiesClient.UpdateTenantAsync(dto);
        if (updated.IsSuccess && updated.Value is not null)
        {
            Tenant = updated.Value;
        }
    }

    public void Dispose()
    {
        TenantService.Metadata?.OnUpdate -= OnUpdatedMetadata;
        GC.SuppressFinalize(this);
    }
}
