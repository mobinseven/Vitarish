using Vitarish.Modules.Base;

namespace Vitarish.Appointments.Interface.Components.Base;

public class SessionsBase : TenantCommonBase
{
    [Inject] protected IAccessControlledSessionManager SessionManager { get; set; } = default!;
    [Inject] protected IAppointmentReminderService AppointmentReminderService { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<SessionReadDto>();
        RenderForType<SessionReminderPolicyReadDto>();
    }
}
