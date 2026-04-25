using System.Linq.Expressions;

namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class AppointmentRepository(AppointmentsDbContext dbContext)
    : EntityRepository<Appointment, AppointmentDescriptor, AppointmentsDbContext>(dbContext),
      IEntityRepository<Appointment, AppointmentDescriptor>
{
    protected override IQueryable<Appointment> Apply(IQueryable<Appointment> query, AppointmentDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.Status is { } status)
        {
            query = query.Where(a => a.Status == status);
        }

        if (descriptor.DateFrom is { } dateFrom)
        {
            query = query.Where(a => a.ScheduledAt >= dateFrom);
        }

        if (descriptor.DateTo is { } dateTo)
        {
            query = query.Where(a => a.ScheduledAt <= dateTo);
        }

        if (descriptor.SortBy is { } sort)
        {
            query = (sort, descriptor.Descending) switch
            {
                (AppointmentSortBy.ScheduledAt, true) => query.OrderByDescending(a => a.ScheduledAt),
                (AppointmentSortBy.ScheduledAt, false) => query.OrderBy(a => a.ScheduledAt),
                (AppointmentSortBy.Status, true) => query.OrderByDescending(a => a.Status),
                (AppointmentSortBy.Status, false) => query.OrderBy(a => a.Status),
                _ => query
            };
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
