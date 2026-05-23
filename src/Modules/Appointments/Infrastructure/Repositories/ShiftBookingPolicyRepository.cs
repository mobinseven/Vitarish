using Vitarish.Appointments.Application.Abstractions.BookingPolicies;
namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class ShiftBookingPolicyRepository(AppointmentsDbContext dbContext) : IShiftBookingPolicyRepository
{
    public Task<Shift?> GetShiftWithPoliciesAsync(Guid shiftId, CancellationToken cancellationToken) =>
        dbContext.Shifts.Include(x => x.BookingPolicies).FirstOrDefaultAsync(x => x.Id == shiftId, cancellationToken);

    public Task SaveAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
