namespace Vitarish.Appointments.Application.Abstractions;

public interface IServiceProviderManager : IEntityService<ServiceProviderReadDto, ServiceProviderWriteDto, ServiceProviderDescriptorDto>
{
    Task<EntityResult> LinkUserAsync(Guid serviceProviderId, string userId, CancellationToken cancellationToken);
    Task<EntityResult> UnlinkUserAsync(Guid serviceProviderId, string userId, CancellationToken cancellationToken);
}
