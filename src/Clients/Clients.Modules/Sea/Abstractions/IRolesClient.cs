using Vitarish.Clients.Modules.Sea.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IRolesClient
{
    Task<Guid> AddAsync(string name, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<RoleDto> Items, int TotalItems)> GetPagedAsync(RoleDescriptorDto descriptor, CancellationToken cancellationToken = default);
}
