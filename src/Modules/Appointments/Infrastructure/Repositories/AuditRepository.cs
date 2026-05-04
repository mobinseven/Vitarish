namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class AuditRepository(AppointmentsDbContext dbContext)
    : EntityRepository<AppointmentAuditEntry, EntityDescriptor, AppointmentsDbContext>(dbContext)
{
    public async Task<IReadOnlyList<AppointmentAuditEntry>> GetByEntityAsync(
        string entityType, Guid entityId, CancellationToken cancellationToken)
        => await Set.AsNoTracking()
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
