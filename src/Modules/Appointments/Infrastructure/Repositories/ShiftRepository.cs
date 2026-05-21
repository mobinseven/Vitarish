namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class ShiftRepository(AppointmentsDbContext dbContext)
    : EntityRepository<Shift, ShiftDescriptor, AppointmentsDbContext>(dbContext),
      IEntityRepository<Shift, ShiftDescriptor>
{
    protected override IQueryable<Shift> Apply(IQueryable<Shift> query, ShiftDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.Status is { } status)
        {
            query = query.Where(s => s.Status == status);
        }

        if (descriptor.Visibility is { } visibility)
        {
            query = query.Where(s => s.Visibility == visibility);
        }

        if (descriptor.DayOfWeek is { } dayOfWeek)
        {
            query = query.Where(s => s.DayOfWeek == dayOfWeek);
        }

        if (descriptor.StartTime is { } startTime)
        {
            query = query.Where(s => s.StartTime == startTime);
        }

        if (descriptor.EndTime is { } endTime)
        {
            query = query.Where(s => s.EndTime == endTime);
        }

        if (descriptor.SortBy is { } sort)
        {
            query = (sort, descriptor.Descending) switch
            {
                (ShiftSortBy.Status, true) => query.OrderByDescending(s => s.Status),
                (ShiftSortBy.Status, false) => query.OrderBy(s => s.Status),
                (ShiftSortBy.Visibility, true) => query.OrderByDescending(s => s.Visibility),
                (ShiftSortBy.Visibility, false) => query.OrderBy(s => s.Visibility),
                _ => query
            };
        }

        if (descriptor.ServiceProviderId is { } serviceProviderId)
        {
            query = query.Where(s => s.ServiceProviderId == serviceProviderId);
        }

        if (descriptor.ServiceId is { } serviceId)
        {
            query = query.Where(s => s.ServiceId == serviceId);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;

    async Task<Shift?> IEntityRepository<Shift, ShiftDescriptor>.GetAsync(
        Guid id, CancellationToken cancellationToken)
    {
        Shift? shift = await dbContext.Shifts
            .AsNoTracking()
            .Include(s => s.BookingPolicies)
            .Include(s => s.ReminderPolicies)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        return shift;
    }

    //async Task<IReadOnlyList<Shift>> IEntityRepository<Shift, ShiftDescriptor>.GetAsync(
    //    ShiftDescriptor descriptor, CancellationToken cancellationToken)
    //{
    //    IQueryable<Shift> query = Apply(
    //        dbContext.Shifts
    //            .AsNoTracking()
    //            .Include(s => s.BookingPolicy)
    //            .Include(s => s.ReminderPolicies),
    //        descriptor);

    //    if (query is not IOrderedQueryable<Shift>)
    //    {
    //        query = query.OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt);
    //    }

    //    IReadOnlyList<Shift> shifts = await query.ApplyPagination(descriptor).ToListAsync(cancellationToken);
    //    return shifts;
    //}
}
