namespace Vitarish.Sea.Interface.Components.Base;

public class UsersBase : SeaCommonBase
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<UserRoleReadDto>();
    }
}
