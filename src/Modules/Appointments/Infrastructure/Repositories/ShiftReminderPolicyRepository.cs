namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class ShiftReminderPolicyRepository(AppointmentsDbContext dbContext)
    : EntityRepository<ShiftReminderPolicy, ShiftReminderPolicyDescriptor, AppointmentsDbContext>(dbContext),
      IEntityRepository<ShiftReminderPolicy, ShiftReminderPolicyDescriptor>
{
    protected override IQueryable<ShiftReminderPolicy> Apply(IQueryable<ShiftReminderPolicy> query, ShiftReminderPolicyDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.ShiftId is not null)
        {
            query = query.Where(p => p.ShiftId == descriptor.ShiftId);
        }

        if (descriptor.IsEnabled is not null)
        {
            query = query.Where(p => p.IsEnabled == descriptor.IsEnabled);
        }

        if (descriptor.Channel is not null)
        {
            query = query.Where(p => p.Channel == descriptor.Channel);
        }

        return query;
    }
}
