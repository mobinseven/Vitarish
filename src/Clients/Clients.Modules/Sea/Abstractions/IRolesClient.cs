using Vitarish.Clients.Modules.Sea.Dto;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IRolesClient : IEntityClient<RoleDto, RoleDescriptorDto>
{
    Task<Guid> AddAsync(string name, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<RoleDto> Items, int TotalItems)> GetPagedAsync(RoleDescriptorDto descriptor, CancellationToken cancellationToken = default);
}
