namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class ReservationRepository(AppointmentsDbContext dbContext)
    : EntityRepository<Reservation, ReservationDescriptor, AppointmentsDbContext>(dbContext),
      IEntityRepository<Reservation, ReservationDescriptor>
{
    protected override IQueryable<Reservation> Apply(IQueryable<Reservation> query, ReservationDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.Status is { } status)
        {
            query = query.Where(r => r.Status == status);
        }

        if (descriptor.SessionId is { } sessionId)
        {
            query = query.Where(r => r.SessionId == sessionId);
        }

        if (descriptor.Date is { } date)
        {
            query = query.Where(r => r.Date == date);
        }

        if (descriptor.ReservingPartyId is { } partyId)
        {
            query = query.Where(r => r.ReservingPartyId == partyId);
        }

        if (descriptor.IsHold is { } isHold)
        {
            query = query.Where(r => r.IsHold == isHold);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;

    Task<Reservation?> IEntityRepository<Reservation, ReservationDescriptor>.GetAsync(
        Guid id, CancellationToken cancellationToken)
    {
        return Set.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    async Task<IReadOnlyList<Reservation>> IEntityRepository<Reservation, ReservationDescriptor>.GetAsync(
        ReservationDescriptor descriptor, CancellationToken cancellationToken)
    {
        IQueryable<Reservation> query = Apply(Set.AsNoTracking(), descriptor);

        if (query is not IOrderedQueryable<Reservation>)
        {
            query = query.OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt);
        }

        return await query.ApplyPagination(descriptor).ToListAsync(cancellationToken);
    }
}
