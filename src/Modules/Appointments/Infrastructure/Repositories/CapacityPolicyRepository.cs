namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class CapacityPolicyRepository(AppointmentsDbContext dbContext)
    : EntityRepository<CapacityPolicy, EntityDescriptor, AppointmentsDbContext>(dbContext)
{
    public async Task<CapacityPolicy?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken)
        => await Set.AsNoTracking()
            .FirstOrDefaultAsync(p => p.SessionId == sessionId, cancellationToken);

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
