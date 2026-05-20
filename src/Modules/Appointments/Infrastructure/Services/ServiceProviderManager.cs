namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ServiceProviderManager(IMediator mediator, AppointmentsDbContext dbContext)
    : EntityService<AppointmentServiceProvider, ServiceProviderDescriptor, ServiceProviderReadDto, ServiceProviderWriteDto, ServiceProviderDescriptorDto>(
        mediator,
        provider => provider.ToDto(),
        dto => dto.ToDomain(),
        dto => dto.ToDomain()),
      IServiceProviderManager
{
    private const string ProviderNotFound = "ServiceProviderNotFoundError";

    public async Task<EntityResult> LinkUserAsync(Guid serviceProviderId, string userId, CancellationToken cancellationToken)
    {
        AppointmentServiceProvider? provider = await dbContext.ServiceProviders
            .Include(sp => sp.UserLinks)
            .FirstOrDefaultAsync(sp => sp.Id == serviceProviderId, cancellationToken);

        if (provider is null)
        {
            return EntityResult.Failure(EntityError.NotFound(ProviderNotFound, nameof(serviceProviderId)));
        }

        if (provider.UserLinks.Any(l => l.UserId == userId))
        {
            return EntityResult.Success();
        }

        provider.UserLinks.Add(new ServiceProviderUserLink { ServiceProviderId = serviceProviderId, UserId = userId });
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return EntityResult.Success();
    }

    public async Task<EntityResult> UnlinkUserAsync(Guid serviceProviderId, string userId, CancellationToken cancellationToken)
    {
        AppointmentServiceProvider? provider = await dbContext.ServiceProviders
            .Include(sp => sp.UserLinks)
            .FirstOrDefaultAsync(sp => sp.Id == serviceProviderId, cancellationToken);

        if (provider is null)
        {
            return EntityResult.Failure(EntityError.NotFound(ProviderNotFound, nameof(serviceProviderId)));
        }

        ServiceProviderUserLink? link = provider.UserLinks.FirstOrDefault(l => l.UserId == userId);
        if (link is null)
        {
            return EntityResult.Success();
        }

        _ = provider.UserLinks.Remove(link);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return EntityResult.Success();
    }
}
