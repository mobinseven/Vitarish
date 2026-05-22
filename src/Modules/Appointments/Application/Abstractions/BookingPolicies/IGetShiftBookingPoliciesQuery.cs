namespace Vitarish.Appointments.Application.Abstractions.BookingPolicies;

public interface IGetShiftBookingPoliciesQuery
{
    Task<IReadOnlyList<ShiftBookingPolicyReadDto>> ExecuteAsync(Guid shiftId, CancellationToken cancellationToken);
}
