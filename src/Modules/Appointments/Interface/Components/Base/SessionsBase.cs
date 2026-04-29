using Vitarish.Modules.Base;

namespace Vitarish.Appointments.Interface.Components.Base;

public class SessionsBase : TenantCommonBase
{
    [Inject] protected IAccessControlledSessionsService SessionsService { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<AppointmentSessionReadDto>();
    }
}
