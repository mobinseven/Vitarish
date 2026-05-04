using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Sea.Application.Abstractions;

public interface IUserRolesService : IEntityService<UserRoleReadDto, UserRoleWriteDto, UserRoleDescriptorDto>
{
    Task AssignAsync(Guid userId, Guid roleId, RoleType type, CancellationToken cancellationToken);
    Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken);
}
