using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Application.Abstractions;

public interface IRolesService : IEntityService<RoleDto, RoleDto, RoleDescriptorDto>
{
    Task<Guid> AddAsync(string name, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken);
    Task<(IReadOnlyList<RoleDto> Items, int TotalItems)> GetPagedAsync(RoleDescriptorDto descriptor, CancellationToken cancellationToken);
}
