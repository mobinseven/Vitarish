using Vitarish.Modules.Base;

namespace Vitarish.Appointments.Interface.Components.Base;

public class ServicesBase : TenantCommonBase
{
    [Inject] protected IServiceManager ServiceManager { get; set; } = default!;
    [Inject] protected IServiceTypeManager ServiceTypeManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ServiceReadDto>();
    }
}
