namespace Vitarish.Sea.Interface.Components.Roles;

public class RolesBase : SeaCommonBase
{
    [Inject] protected IRolesService RolesService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<RoleDto>();
    }
}
