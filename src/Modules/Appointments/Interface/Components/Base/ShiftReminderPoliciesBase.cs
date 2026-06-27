using Vitarish.Modules.Base.Interface;

namespace Vitarish.Appointments.Interface.Components.Base;

public class ShiftReminderPoliciesBase : TenantCommonBase
{
    [Inject] protected IShiftReminderPolicyManager ReminderPolicyManager { get; set; } = default!;
}
