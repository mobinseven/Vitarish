using Vitarish.Modules.Base;

namespace Vitarish.Appointments.Interface.Components.Base;

public class ShiftBookingPoliciesBase : TenantCommonBase
{
    [Inject] protected IShiftBookingPolicyManager BookingPolicyManager { get; set; } = default!;
    [Inject] protected IAccessControlledShiftManager ShiftManager { get; set; } = default!;
}
