namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftAggregateLoader(AppointmentsDbContext dbContext) : IShiftAggregateLoader
{
    public Task<Shift?> LoadWithPoliciesAsync(Guid shiftId, CancellationToken cancellationToken) =>
        dbContext.Shifts
            .Include(s => s.ReminderPolicies)
            .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);

    public async Task SaveAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
