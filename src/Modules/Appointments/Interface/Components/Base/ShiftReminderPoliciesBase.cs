using Vitarish.Modules.Aggregator.Core;

namespace Vitarish.Appointments.Interface.Components.Base;

public class ShiftReminderPoliciesBase : TenantCommonBase
{
    [Inject] protected IShiftReminderPolicyManager ReminderPolicyManager { get; set; } = default!;
}
