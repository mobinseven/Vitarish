namespace Vitarish.Clients.Interface.Sea.Components.Users;

public class UsersBase : SeaCommonBase
{
    [Inject] protected IUserRolesClient UserRolesService { get; set; } = default!;
    [Inject] protected IRolesClient RolesService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<UserRoleDto>();
    }
}

