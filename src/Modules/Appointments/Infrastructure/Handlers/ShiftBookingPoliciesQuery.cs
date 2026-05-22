using MediatR;
using Vitarish.Appointments.Application.Abstractions.BookingPolicies;
namespace Vitarish.Appointments.Infrastructure.Handlers;

[RegisterTransient]
internal sealed class ShiftBookingPoliciesQuery(ISender sender) : IGetShiftBookingPoliciesQuery
{
    public async Task<IReadOnlyList<ShiftBookingPolicyReadDto>> ExecuteAsync(Guid shiftId, CancellationToken cancellationToken)
    {
        Shift? shift = await sender.Send(new GetShiftWithPoliciesCommand(shiftId), cancellationToken);
        return shift is null ? [] : [.. shift.BookingPolicies.Select(x => x.ToDto())];
    }
}
