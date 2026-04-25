using Vitarish.Modules.Base;

namespace Vitarish.Appointments.Interface.Components.Base;

public class AppointmentsBase : TenantCommonBase
{
    [Inject] protected IAccessControlledAppointmentsService AppointmentsService { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<AppointmentReadDto>();
    }
}
