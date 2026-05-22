using MediatR;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal sealed record GetShiftWithPoliciesCommand(Guid ShiftId) : IRequest<Shift?>;
internal sealed record SaveShiftBookingPoliciesCommand : IRequest;

[RegisterTransient]
internal sealed class ShiftBookingPolicyDataHandler(AppointmentsDbContext dbContext)
    : IRequestHandler<GetShiftWithPoliciesCommand, Shift?>,
      IRequestHandler<SaveShiftBookingPoliciesCommand>
{
    public Task<Shift?> Handle(GetShiftWithPoliciesCommand request, CancellationToken cancellationToken) =>
        dbContext.Shifts.Include(x => x.BookingPolicies).FirstOrDefaultAsync(x => x.Id == request.ShiftId, cancellationToken);

    public async Task Handle(SaveShiftBookingPoliciesCommand request, CancellationToken cancellationToken) =>
        _ = await dbContext.SaveChangesAsync(cancellationToken);
}
