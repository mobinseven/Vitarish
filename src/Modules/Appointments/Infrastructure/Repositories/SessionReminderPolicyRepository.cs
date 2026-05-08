namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class SessionReminderPolicyRepository(AppointmentsDbContext dbContext)
    : EntityRepository<SessionReminderPolicy, SessionReminderPolicyDescriptor, AppointmentsDbContext>(dbContext),
      ISessionReminderPolicyRepository
{
    protected override IQueryable<SessionReminderPolicy> Apply(
        IQueryable<SessionReminderPolicy> query,
        SessionReminderPolicyDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.SessionId is { } sessionId)
        {
            query = query.Where(p => p.SessionId == sessionId);
        }

        if (descriptor.IsEnabled is { } isEnabled)
        {
            query = query.Where(p => p.IsEnabled == isEnabled);
        }

        if (descriptor.Channel is { } channel)
        {
            query = query.Where(p => p.Channel == channel);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;

    public async Task<IReadOnlyList<SessionReminderPolicy>> GetBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        List<SessionReminderPolicy> result = await Set.AsNoTracking()
            .Where(p => p.SessionId == sessionId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return result;
    }

    public async Task<IReadOnlyList<SessionReminderPolicy>> GetEnabledBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        List<SessionReminderPolicy> result = await Set.AsNoTracking()
            .Where(p => p.SessionId == sessionId && p.IsEnabled)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return result;
    }
}
