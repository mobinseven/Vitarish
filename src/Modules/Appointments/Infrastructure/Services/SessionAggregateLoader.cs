namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionAggregateLoader(AppointmentsDbContext dbContext) : ISessionAggregateLoader
{
    public Task<Session?> LoadWithPoliciesAsync(Guid sessionId, CancellationToken cancellationToken) =>
        dbContext.Sessions
            .Include(s => s.ReminderPolicies)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

    public async Task SaveAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
