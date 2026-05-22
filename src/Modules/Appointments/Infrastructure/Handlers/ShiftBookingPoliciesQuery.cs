using Vitarish.Appointments.Application.Abstractions.BookingPolicies;
namespace Vitarish.Appointments.Infrastructure.Handlers;

[RegisterTransient]
internal sealed class ShiftBookingPoliciesQuery(IShiftBookingPolicyRepository repository) : IGetShiftBookingPoliciesQuery
{
    public async Task<IReadOnlyList<ShiftBookingPolicyReadDto>> ExecuteAsync(Guid shiftId, CancellationToken cancellationToken)
    {
        Shift? shift = await repository.GetShiftWithPoliciesAsync(shiftId, cancellationToken);
        return shift is null ? [] : [.. shift.BookingPolicies.Select(x => x.ToDto())];
    }
}
