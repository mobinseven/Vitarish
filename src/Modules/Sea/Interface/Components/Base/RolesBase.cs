namespace Vitarish.Sea.Interface.Components.Base;

public class RolesBase : SeaCommonBase
{
    [Inject] protected IRolesService RolesService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<RoleDto>();
    }
}
