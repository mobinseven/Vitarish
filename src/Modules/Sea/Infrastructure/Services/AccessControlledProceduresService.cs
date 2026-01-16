using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Sea.Infrastructure.Services;

internal class AccessControlledProceduresService(
    IRequestContext requestContext,
    IEventRolesService eventRolesService,
    IUserRolesService userRolesService,
    IProceduresService proceduresService) : AccessControlledServiceBase(requestContext, eventRolesService, userRolesService), IAccessControlledProceduresService
{
    private IProceduresService Service => proceduresService;

    private async Task<ProcedureDescriptorDto> IncludeRoleIdsInDescriptor(ProcedureDescriptorDto procedureDescriptorDto)
    {
        await EnsureInitialized();
        procedureDescriptorDto.RoleIds = isAdmin ? null : [.. userReadRoleIds];
        return procedureDescriptorDto;
    }

    public async Task<EntityResult<ProcedureDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await EnsureInitialized();
        ProcedureDescriptorDto descriptor = new()
        {
            Id = id,
            RoleIds = isAdmin ? null : [.. userReadRoleIds]
        };
        IReadOnlyList<ProcedureDto> res = await Service.GetListAsync(descriptor, cancellationToken).EnsureValueAsync();
        ProcedureDto? procedure = res.SingleOrDefault();
        return procedure is null ? EntityResult<ProcedureDto>.Failure() : EntityResult<ProcedureDto>.Success(procedure);
    }


    public async Task<Guid> AddAsync(Guid projectId, string name, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddAsync(projectId, name, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<ProcedureDto>> AddAsync(ProcedureWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddAsync(dto, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<IReadOnlyList<ProcedureDto>>> AddAsync(IReadOnlyList<ProcedureWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddAsync(dtos, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<ProcedureDto>> AddOrUpdateAsync(ProcedureWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddOrUpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;
    public async Task<ProcedureSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) => await IsAdmin() ? await Service.CaptureSnapshotAsync(id, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<int>> CountAsync(ProcedureDescriptorDto descriptor, CancellationToken cancellationToken) => await Service.CountAsync(await IncludeRoleIdsInDescriptor(descriptor), cancellationToken);
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await IsAdmin())
        {

            await Service.DeleteAsync(id, cancellationToken);
        }
        else
        {
            throw AdminRoleRequired;
        }
    }
    public async Task<EntityResult<IReadOnlyList<ProcedureDto>>> GetListAsync(ProcedureDescriptorDto descriptor, CancellationToken cancellationToken) => await Service.GetListAsync(await IncludeRoleIdsInDescriptor(descriptor), cancellationToken);
    public async Task<EntityResult<PartedResult<ProcedureDto>>> GetPartedAsync(ProcedureDescriptorDto descriptor, CancellationToken cancellationToken) => await Service.GetPartedAsync(await IncludeRoleIdsInDescriptor(descriptor), cancellationToken);
    public async Task<EntityResult> RemoveAsync(ProcedureWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(dto, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult> RemoveAsync(IReadOnlyList<ProcedureWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(dtos, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(id, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult> RemoveByDescriptorAsync(ProcedureDescriptorDto descriptor, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw AdminRoleRequired;
    public async Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken)
    {
        if (await IsAdmin())
        {

            await Service.UpdateAsync(id, name, cancellationToken);
        }
        else
        {
            throw AdminRoleRequired;
        }
    }
    public async Task<EntityResult<ProcedureDto>> UpdateAsync(ProcedureWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.UpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<IReadOnlyList<ProcedureDto>>> UpdateAsync(IReadOnlyList<ProcedureWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.UpdateAsync(dtos, cancellationToken) : throw AdminRoleRequired;
    public async Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken)
    {
        if (await IsAdmin())
        {

            await Service.UpdateMetadataAsync(id, metadata, cancellationToken);
        }
        else
        {
            throw AdminRoleRequired;
        }
    }
}
