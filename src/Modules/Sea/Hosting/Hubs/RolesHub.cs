using Vitarish.Entities.Server.Core;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class RolesHub(IRolesService service, IEntityUserService? entityUserService = null)
    : EntityServer<RoleDto, RoleWriteDto, RoleDescriptorDto, IRolesService>(service, entityUserService)
{
    public Task<Guid> CreateRoleAsync(string name, CancellationToken cancellationToken) =>
        EntityService.AddAsync(name, cancellationToken);

    public Task UpdateRoleNameAsync(Guid id, string name, CancellationToken cancellationToken) =>
        EntityService.UpdateAsync(id, name, cancellationToken);

    public Task<(IReadOnlyList<RoleDto> Items, int TotalItems)> GetPagedAsync(RoleDescriptorDto descriptor, CancellationToken cancellationToken) =>
        EntityService.GetPagedAsync(descriptor, cancellationToken);
}
