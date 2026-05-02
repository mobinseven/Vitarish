using Vitarish.Modules.Base;

namespace Vitarish.Appointments.Interface.Components.Base;

public class AppointmentSessionsBase : TenantCommonBase
{
    [Inject] protected IAccessControlledAppointmentSessionsService SessionsService { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<AppointmentSessionReadDto>();
    }
}
