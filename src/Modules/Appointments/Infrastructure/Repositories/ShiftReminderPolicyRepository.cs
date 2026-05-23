namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class ShiftReminderPolicyRepository(AppointmentsDbContext dbContext)
    : EntityRepository<ShiftReminderPolicy, ShiftReminderPolicyDescriptor, AppointmentsDbContext>(dbContext),
      IEntityRepository<ShiftReminderPolicy, ShiftReminderPolicyDescriptor>
{
    protected override IQueryable<ShiftReminderPolicy> Apply(IQueryable<ShiftReminderPolicy> query, ShiftReminderPolicyDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.ShiftId is { } shiftId)
        {
            query = query.Where(p => p.ShiftId == shiftId);
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
}
