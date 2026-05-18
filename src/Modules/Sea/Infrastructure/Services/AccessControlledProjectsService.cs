using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Sea.Infrastructure.Services;

internal class AccessControlledProjectsService(
    IRequestContext requestContext,
    IEventRolesService eventRolesService,
    IUserRolesService userRolesService,
    IProjectsService projectsService) : AccessControlledServiceBase(requestContext, eventRolesService, userRolesService), IAccessControlledProjectsService
{
    private IProjectsService Service => projectsService;

    private async Task<ProjectDescriptorDto> IncludeRoleIdsInDescriptor(ProjectDescriptorDto projectDescriptorDto)
    {
        await EnsureInitialized();
        projectDescriptorDto.RoleIds = ElevatedAccess ? null : [.. userReadRoleIds];
        return projectDescriptorDto;
    }

    public async Task<EntityResult<ProjectReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await EnsureInitialized();
        ProjectDescriptorDto descriptor = new()
        {
            Id = id,
            RoleIds = ElevatedAccess ? null : [.. userReadRoleIds],
            Template = null
        };
        IReadOnlyList<ProjectReadDto> res = await Service.GetListAsync(descriptor, cancellationToken).EnsureValueAsync();
        ProjectReadDto? project = res.SingleOrDefault();
        return project is null ? EntityResult<ProjectReadDto>.Failure() : EntityResult<ProjectReadDto>.Success(project);
    }
    public async Task<EntityResult<ProjectReadDto>> AddAsync(ProjectWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddAsync(dto, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult<IReadOnlyList<ProjectReadDto>>> AddAsync(IReadOnlyList<ProjectWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult<ProjectReadDto>> AddOrUpdateAsync(ProjectWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddOrUpdateAsync(dto, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult<int>> CountAsync(ProjectDescriptorDto descriptor, CancellationToken cancellationToken) => await Service.CountAsync(await IncludeRoleIdsInDescriptor(descriptor), cancellationToken);
    public async Task<EntityResult<IReadOnlyList<ProjectReadDto>>> GetListAsync(ProjectDescriptorDto descriptor, CancellationToken cancellationToken) => await Service.GetListAsync(await IncludeRoleIdsInDescriptor(descriptor), cancellationToken);
    public async Task<EntityResult<PartedResult<ProjectReadDto>>> GetPartedAsync(ProjectDescriptorDto descriptor, CancellationToken cancellationToken) => await Service.GetPartedAsync(await IncludeRoleIdsInDescriptor(descriptor), cancellationToken);
    public async Task<EntityResult> RemoveAsync(ProjectWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(dto, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult> RemoveAsync(IReadOnlyList<ProjectWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(id, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult> RemoveByDescriptorAsync(ProjectDescriptorDto descriptor, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult<ProjectReadDto>> UpdateAsync(ProjectWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.UpdateAsync(dto, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult<IReadOnlyList<ProjectReadDto>>> UpdateAsync(IReadOnlyList<ProjectWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.UpdateAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;

}
