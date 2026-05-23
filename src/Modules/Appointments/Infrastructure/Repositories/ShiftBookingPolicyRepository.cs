namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class ShiftBookingPolicyRepository(AppointmentsDbContext dbContext)
    : EntityRepository<ShiftBookingPolicy, ShiftBookingPolicyDescriptor, AppointmentsDbContext>(dbContext),
      IEntityRepository<ShiftBookingPolicy, ShiftBookingPolicyDescriptor>
{
    protected override IQueryable<ShiftBookingPolicy> Apply(IQueryable<ShiftBookingPolicy> query, ShiftBookingPolicyDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.ShiftId is { } shiftId)
        {
            query = query.Where(p => p.ShiftId == shiftId);
        }

        if (descriptor.Date is { } date)
        {
            query = query.Where(p => p.Date == date);
        }

        if (descriptor.Closed is { } closed)
        {
            query = query.Where(p => p.Closed == closed);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
