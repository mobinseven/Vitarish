namespace Vitarish.Clients.Interface.Sea.Components.Roles;

public class RolesBase : SeaCommonBase
{
    [Inject] protected IRolesClient RolesService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<RoleDto>();
    }
}
