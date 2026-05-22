namespace Vitarish.Appointments.Application.Abstractions.BookingPolicies;

public interface IRemoveShiftBookingPolicyCommand
{
    Task<EntityResult> ExecuteAsync(RemoveShiftBookingPolicyCommandModel model, CancellationToken cancellationToken);
}
