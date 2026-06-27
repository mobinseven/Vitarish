using Vitarish.Modules.Aggregator.Core;

namespace Vitarish.Appointments.Interface.Components.Base;

public class ShiftsBase : TenantCommonBase
{
    [Inject] protected IAccessControlledShiftManager ShiftManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ShiftReadDto>();
    }
}
