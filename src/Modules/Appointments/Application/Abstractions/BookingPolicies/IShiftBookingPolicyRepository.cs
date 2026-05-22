namespace Vitarish.Appointments.Application.Abstractions.BookingPolicies;

public interface IShiftBookingPolicyRepository
{
    Task<Shift?> GetShiftWithPoliciesAsync(Guid shiftId, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}
