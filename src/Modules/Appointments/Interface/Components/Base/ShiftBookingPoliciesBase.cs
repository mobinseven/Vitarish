using Vitarish.Appointments.Application.Abstractions.BookingPolicies;
using Vitarish.Modules.Base;

namespace Vitarish.Appointments.Interface.Components.Base;

public class ShiftBookingPoliciesBase : TenantCommonBase
{
    [Inject] protected IGetShiftBookingPoliciesQuery BookingPoliciesQuery { get; set; } = default!;
    [Inject] protected IUpsertShiftBookingPolicyCommand UpsertBookingPolicyCommand { get; set; } = default!;
    [Inject] protected IRemoveShiftBookingPolicyCommand RemoveBookingPolicyCommand { get; set; } = default!;
    [Inject] protected IAccessControlledShiftManager ShiftManager { get; set; } = default!;
}
