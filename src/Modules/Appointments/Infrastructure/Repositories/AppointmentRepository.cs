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

        if (descriptor.SessionId is { } sessionId)
        {
            query = query.Where(a => a.SessionId == sessionId);
        }

        if (descriptor.UserId is { } userId)
        {
            query = query.Where(a => a.UserId == userId);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;

    // Explicit interface implementations add eager-loading of Session to every read path.

    async Task<Appointment?> IEntityRepository<Appointment, AppointmentDescriptor>.GetAsync(
        Guid id, CancellationToken cancellationToken)
    {
        return await Set.AsNoTracking()
            .Include(a => a.Session)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    async Task<IReadOnlyList<Appointment>> IEntityRepository<Appointment, AppointmentDescriptor>.GetAsync(
        AppointmentDescriptor descriptor, CancellationToken cancellationToken)
    {
        IQueryable<Appointment> query = Apply(Set.AsNoTracking().Include(a => a.Session), descriptor);

        if (query is not IOrderedQueryable<Appointment>)
        {
            query = query.OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt);
        }

        return await query.ApplyPagination(descriptor).ToListAsync(cancellationToken);
    }
}
