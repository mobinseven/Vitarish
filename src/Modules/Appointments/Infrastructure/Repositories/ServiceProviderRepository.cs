namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class ServiceProviderRepository(AppointmentsDbContext dbContext)
    : EntityRepository<AppointmentServiceProvider, ServiceProviderDescriptor, AppointmentsDbContext>(dbContext),
      IEntityRepository<AppointmentServiceProvider, ServiceProviderDescriptor>
{
    protected override IQueryable<AppointmentServiceProvider> Apply(IQueryable<AppointmentServiceProvider> query, ServiceProviderDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (!string.IsNullOrWhiteSpace(descriptor.Name))
        {
            query = query.Where(sp => sp.Name.Contains(descriptor.Name));
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;

    Task<AppointmentServiceProvider?> IEntityRepository<AppointmentServiceProvider, ServiceProviderDescriptor>.GetAsync(
        Guid id, CancellationToken cancellationToken)
        => Set.AsNoTracking()
              .Include(sp => sp.UserLinks)
              .FirstOrDefaultAsync(sp => sp.Id == id, cancellationToken);

    async Task<IReadOnlyList<AppointmentServiceProvider>> IEntityRepository<AppointmentServiceProvider, ServiceProviderDescriptor>.GetAsync(
        ServiceProviderDescriptor descriptor, CancellationToken cancellationToken)
    {
        IQueryable<AppointmentServiceProvider> query = Apply(
            Set.AsNoTracking().Include(sp => sp.UserLinks),
            descriptor);

        if (query is not IOrderedQueryable<AppointmentServiceProvider>)
        {
            query = query.OrderBy(sp => sp.Name);
        }

        return await query.ApplyPagination(descriptor).ToListAsync(cancellationToken);
    }
}
