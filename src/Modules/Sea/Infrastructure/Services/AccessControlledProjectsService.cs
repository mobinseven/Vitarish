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
        projectDescriptorDto.RoleIds = isAdmin ? null : [.. userReadRoleIds];
        return projectDescriptorDto;
    }

    public async Task<EntityResult<ProjectDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await EnsureInitialized();
        ProjectDescriptorDto descriptor = new()
        {
            Id = id,
            RoleIds = isAdmin ? null : [.. userReadRoleIds],
            Template = null
        };
        IReadOnlyList<ProjectDto> res = await Service.GetListAsync(descriptor, cancellationToken).EnsureValueAsync();
        ProjectDto? project = res.SingleOrDefault();
        return project is null ? EntityResult<ProjectDto>.Failure() : EntityResult<ProjectDto>.Success(project);
    }

    public async Task<Guid> AddAsync(string name, string description, bool template, string metadata, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddAsync(name, description, template, metadata, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<ProjectDto>> AddAsync(ProjectWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddAsync(dto, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<IReadOnlyList<ProjectDto>>> AddAsync(IReadOnlyList<ProjectWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddAsync(dtos, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<ProjectDto>> AddOrUpdateAsync(ProjectWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddOrUpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;
    public async Task<ProjectSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) => await IsAdmin() ? await Service.CaptureSnapshotAsync(id, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<int>> CountAsync(ProjectDescriptorDto descriptor, CancellationToken cancellationToken) => await Service.CountAsync(await IncludeRoleIdsInDescriptor(descriptor), cancellationToken);
    public async Task<EntityResult<IReadOnlyList<ProjectDto>>> GetListAsync(ProjectDescriptorDto descriptor, CancellationToken cancellationToken) => await Service.GetListAsync(await IncludeRoleIdsInDescriptor(descriptor), cancellationToken);
    public async Task<EntityResult<PartedResult<ProjectDto>>> GetPartedAsync(ProjectDescriptorDto descriptor, CancellationToken cancellationToken) => await Service.GetPartedAsync(await IncludeRoleIdsInDescriptor(descriptor), cancellationToken);
    public async Task<EntityResult> RemoveAsync(ProjectWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(dto, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult> RemoveAsync(IReadOnlyList<ProjectWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(dtos, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(id, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult> RemoveByDescriptorAsync(ProjectDescriptorDto descriptor, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw AdminRoleRequired;
    public Task UpdateAsync(Guid id, string name, string description, bool template, string metadata, CancellationToken cancellationToken) => Service.UpdateAsync(id, name, description, template, metadata, cancellationToken);
    public async Task<EntityResult<ProjectDto>> UpdateAsync(ProjectWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.UpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<IReadOnlyList<ProjectDto>>> UpdateAsync(IReadOnlyList<ProjectWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.UpdateAsync(dtos, cancellationToken) : throw AdminRoleRequired;
    public Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken) => Service.UpdateMetadataAsync(id, metadata, cancellationToken);
}
