using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Commerce.Application.Abstractions;

public interface ICustomerProfileService : IEntityService<CustomerProfileReadDto, CustomerProfileWriteDto, CustomerProfileDescriptorDto>
{
    Task<CustomerProfileReadDto?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task<CustomerProfileReadDto> EnsureProfileAsync(string userId, CancellationToken cancellationToken);
}
