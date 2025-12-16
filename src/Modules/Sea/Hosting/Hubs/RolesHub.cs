using Vitarish.Entities.Server.Core;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class RolesHub : EntityServer<RoleDto, RoleWriteDto, RoleDescriptorDto, IRolesService>
{
    private readonly IRolesService _service;

    public RolesHub(IServiceScopeFactory serviceScopeFactory, IRolesService service, IEntityUserService? entityUserService = null)
        : base(serviceScopeFactory, entityUserService)
    {
        _service = service;
    }

    public Task<Guid> CreateRoleAsync(string name, CancellationToken cancellationToken) =>
        _service.AddAsync(name, cancellationToken);

    public Task UpdateRoleNameAsync(Guid id, string name, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, name, cancellationToken);

    public Task<(IReadOnlyList<RoleDto> Items, int TotalItems)> GetPagedAsync(RoleDescriptorDto descriptor, CancellationToken cancellationToken) =>
        _service.GetPagedAsync(descriptor, cancellationToken);
}
