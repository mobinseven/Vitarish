namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class ServiceTypeRepository(AppointmentsDbContext dbContext)
    : EntityRepository<ServiceType, ServiceTypeDescriptor, AppointmentsDbContext>(dbContext),
      IEntityRepository<ServiceType, ServiceTypeDescriptor>
{
    protected override IQueryable<ServiceType> Apply(IQueryable<ServiceType> query, ServiceTypeDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (!string.IsNullOrWhiteSpace(descriptor.Name))
        {
            query = query.Where(t => t.Name.Contains(descriptor.Name));
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;

    Task<ServiceType?> IEntityRepository<ServiceType, ServiceTypeDescriptor>.GetAsync(
        Guid id, CancellationToken cancellationToken)
        => Set.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    async Task<IReadOnlyList<ServiceType>> IEntityRepository<ServiceType, ServiceTypeDescriptor>.GetAsync(
        ServiceTypeDescriptor descriptor, CancellationToken cancellationToken)
    {
        IQueryable<ServiceType> query = Apply(Set.AsNoTracking(), descriptor);

        if (query is not IOrderedQueryable<ServiceType>)
        {
            query = query.OrderBy(t => t.Name);
        }

        return await query.ApplyPagination(descriptor).ToListAsync(cancellationToken);
    }
}
