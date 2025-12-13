using Vorn.Entities;
using Vorn.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class UserRolesHub : EntityServer<UserRoleDto, UserRoleDescriptorDto, IUserRolesService>
{
    private readonly IUserRolesService _service;

    public UserRolesHub(IUserRolesService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task AssignAsync(Guid userId, Guid roleId, RoleType type, CancellationToken cancellationToken) =>
        _service.AssignAsync(userId, roleId, type, cancellationToken);

    public Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken) =>
        _service.RemoveAsync(userId, roleId, cancellationToken);
}
