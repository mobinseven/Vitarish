namespace Vitarish.Sea.Interface.Components.Base;

public class UsersBase : SeaCommonBase
{
    [Inject] protected IUserRolesService UserRolesService { get; set; } = default!;
    [Inject] protected IRolesService RolesService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<UserRoleDto>();
    }
}

