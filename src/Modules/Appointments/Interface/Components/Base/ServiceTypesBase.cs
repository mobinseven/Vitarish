using Vitarish.Modules.Base;

namespace Vitarish.Appointments.Interface.Components.Base;

public class ServiceTypesBase : TenantCommonBase
{
    [Inject] protected IServiceTypeManager ServiceTypeManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ServiceTypeReadDto>();
    }
}
