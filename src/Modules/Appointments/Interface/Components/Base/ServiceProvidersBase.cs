using Vitarish.Modules.Base.Interface;

namespace Vitarish.Appointments.Interface.Components.Base;

public class ServiceProvidersBase : TenantCommonBase
{
    [Inject] protected IServiceProviderManager ServiceProviderManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ServiceProviderReadDto>();
    }
}
