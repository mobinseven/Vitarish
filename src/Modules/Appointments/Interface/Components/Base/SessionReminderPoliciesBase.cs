using Vitarish.Modules.Base;

namespace Vitarish.Appointments.Interface.Components.Base;

public class SessionReminderPoliciesBase : TenantCommonBase
{
    [Inject] protected ISessionReminderPolicyManager ReminderPolicyManager { get; set; } = default!;
}
