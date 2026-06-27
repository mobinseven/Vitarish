using Vitarish.Modules.Aggregator.Core;

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
