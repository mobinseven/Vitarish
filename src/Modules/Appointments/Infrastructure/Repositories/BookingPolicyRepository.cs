namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class BookingPolicyRepository(AppointmentsDbContext dbContext)
    : EntityRepository<BookingPolicy, EntityDescriptor, AppointmentsDbContext>(dbContext)
{
    public async Task<BookingPolicy?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken)
        => await Set.AsNoTracking()
            .FirstOrDefaultAsync(p => p.SessionId == sessionId, cancellationToken);

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
