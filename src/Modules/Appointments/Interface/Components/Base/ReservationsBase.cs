using Vitarish.Modules.Aggregator.Core;

namespace Vitarish.Appointments.Interface.Components.Base;

public class ReservationsBase : TenantCommonBase
{
    [Inject] protected IAccessControlledReservationManager ReservationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ReservationReadDto>();
    }
}
