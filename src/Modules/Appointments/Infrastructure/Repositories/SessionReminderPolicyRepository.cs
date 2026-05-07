namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class SessionReminderPolicyRepository(AppointmentsDbContext dbContext)
    : EntityRepository<SessionReminderPolicy, SessionReminderPolicyDescriptor, AppointmentsDbContext>,
      IEntityRepository<SessionReminderPolicy, SessionReminderPolicyDescriptor>
{
    protected override IQueryable<SessionReminderPolicy> Apply(IQueryable<SessionReminderPolicy> query, SessionReminderPolicyDescriptor descriptor)
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

        if (descriptor.Kind is { } kind)
        {
            query = query.Where(p => p.Kind == kind);
        }

        return query;
    }

    public async Task<IReadOnlyList<SessionReminderPolicy>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken)
        => await Set.AsNoTracking()
            .Where(p => p.SessionId == sessionId)
            .ToListAsync(cancellationToken);

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
