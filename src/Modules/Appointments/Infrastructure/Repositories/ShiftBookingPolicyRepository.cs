namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class ShiftBookingPolicyRepository(AppointmentsDbContext dbContext)
    : EntityRepository<ShiftBookingPolicy, ShiftBookingPolicyDescriptor, AppointmentsDbContext>(dbContext),
      IEntityRepository<ShiftBookingPolicy, ShiftBookingPolicyDescriptor>
{
    protected override IQueryable<ShiftBookingPolicy> Apply(IQueryable<ShiftBookingPolicy> query, ShiftBookingPolicyDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.ShiftId is not null)
        {
            query = query.Where(p => p.ShiftId == descriptor.ShiftId);
        }

        if (descriptor.Date is not null)
        {
            query = query.Where(p => p.Date == descriptor.Date);
        }

        return query;
    }
}
