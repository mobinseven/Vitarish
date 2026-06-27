namespace Vitarish.Sea.Interface.Components.Base;

public class RolesBase : SeaCommonBase
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<RoleReadDto>();
    }
}
