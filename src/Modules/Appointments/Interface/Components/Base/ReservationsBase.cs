using Vitarish.Modules.Base;

namespace Vitarish.Appointments.Interface.Components.Base;

public class ReservationsBase : TenantCommonBase
{
    [Inject] protected IAccessControlledReservationManager ReservationManager { get; set; } = default!;
    [Inject] protected IAppointmentReminderService AppointmentReminderService { get; set; } = default!;

    protected static bool IsTerminal(ReservationStatus status) => status is ReservationStatus.Cancelled
        or ReservationStatus.Rejected
        or ReservationStatus.Expired
        or ReservationStatus.Completed;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ReservationReadDto>();
        RenderForType<ReminderReadDto>();
    }
}
