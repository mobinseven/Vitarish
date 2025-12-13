using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Infrastructure.Extensions;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterScoped]
internal class RolesService(IMediator mediator, RolesMapper mapper) : EntityService<Role, RoleDescriptor, RoleDto, RoleDescriptorDto>(mediator, mapper.ToDto, mapper.ToDomain, mapper.ToDomain), IRolesService
{
    public async Task<Guid> AddAsync(string name, CancellationToken cancellationToken)
    {
        Role role = new(name);
        _ = (await AddEntityAsync(role, cancellationToken)).EnsureValue("Add role");
        return role.Id;
    }

    public async Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken)
    {
        Role? role = (await GetEntityAsync(id, cancellationToken)).GetValueOrDefault();
        if(role is null)
        {
            return;
        }

        role.Name = name;
        _ = (await UpdateEntityAsync(role, cancellationToken)).EnsureValue("Update role");
    }

    public async Task<(IReadOnlyList<RoleDto> Items, int TotalItems)> GetPagedAsync(RoleDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        PartedResult<RoleDto> result = (await GetPartedAsync(descriptor, cancellationToken))
            .EnsureValue("Get paged roles");
        return (result.Items, result.TotalItems);
    }
}
