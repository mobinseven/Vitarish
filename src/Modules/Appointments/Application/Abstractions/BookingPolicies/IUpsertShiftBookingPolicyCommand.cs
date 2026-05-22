namespace Vitarish.Appointments.Application.Abstractions.BookingPolicies;

public interface IUpsertShiftBookingPolicyCommand
{
    Task<EntityResult<ShiftBookingPolicyReadDto>> CreateAsync(UpsertShiftBookingPolicyCommandModel model, CancellationToken cancellationToken);
    Task<EntityResult<ShiftBookingPolicyReadDto>> UpdateAsync(UpsertShiftBookingPolicyCommandModel model, CancellationToken cancellationToken);
}
