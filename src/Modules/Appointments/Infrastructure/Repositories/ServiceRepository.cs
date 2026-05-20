namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class ServiceRepository(AppointmentsDbContext dbContext)
    : EntityRepository<Service, AppointmentServiceDescriptor, AppointmentsDbContext>(dbContext),
      IEntityRepository<Service, AppointmentServiceDescriptor>
{
    protected override IQueryable<Service> Apply(IQueryable<Service> query, AppointmentServiceDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (!string.IsNullOrWhiteSpace(descriptor.Name))
        {
            query = query.Where(s => s.Name.Contains(descriptor.Name));
        }

        if (descriptor.ServiceTypeId is { } serviceTypeId)
        {
            query = query.Where(s => s.ServiceTypeId == serviceTypeId);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;

    Task<Service?> IEntityRepository<Service, AppointmentServiceDescriptor>.GetAsync(
        Guid id, CancellationToken cancellationToken)
        => Set.AsNoTracking()
              .Include(s => s.ServiceType)
              .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    async Task<IReadOnlyList<Service>> IEntityRepository<Service, AppointmentServiceDescriptor>.GetAsync(
        AppointmentServiceDescriptor descriptor, CancellationToken cancellationToken)
    {
        IQueryable<Service> query = Apply(
            Set.AsNoTracking().Include(s => s.ServiceType),
            descriptor);

        if (query is not IOrderedQueryable<Service>)
        {
            query = query.OrderBy(s => s.Name);
        }

        return await query.ApplyPagination(descriptor).ToListAsync(cancellationToken);
    }
}
