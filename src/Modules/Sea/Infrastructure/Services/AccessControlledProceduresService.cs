using Vitarish.Entities.Contracts.Models;
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
        procedureDescriptorDto.RoleIds = ElevatedAccess ? null : [.. userReadRoleIds];
        return procedureDescriptorDto;
    }

    public async Task<EntityResult<ProcedureReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await EnsureInitialized();
        ProcedureDescriptorDto descriptor = new()
        {
            Id = id,
            RoleIds = ElevatedAccess ? null : [.. userReadRoleIds]
        };
        IReadOnlyList<ProcedureReadDto> res = await Service.GetListAsync(descriptor, cancellationToken).EnsureValueAsync();
        ProcedureReadDto? procedure = res.SingleOrDefault();
        return procedure is null ? EntityResult<ProcedureReadDto>.Failure() : EntityResult<ProcedureReadDto>.Success(procedure);
    }

    public async Task<EntityResult<ProcedureReadDto>> AddAsync(ProcedureWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddAsync(dto, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult<IReadOnlyList<ProcedureReadDto>>> AddAsync(IReadOnlyList<ProcedureWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult<ProcedureReadDto>> AddOrUpdateAsync(ProcedureWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddOrUpdateAsync(dto, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult<int>> CountAsync(ProcedureDescriptorDto descriptor, CancellationToken cancellationToken) => await Service.CountAsync(await IncludeRoleIdsInDescriptor(descriptor), cancellationToken);
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await IsAdmin())
        {

            await Service.DeleteAsync(id, cancellationToken);
        }
        else
        {
            throw ElevatedAccessRequired;
        }
    }
    public async Task<EntityResult<IReadOnlyList<ProcedureReadDto>>> GetListAsync(ProcedureDescriptorDto descriptor, CancellationToken cancellationToken) => await Service.GetListAsync(await IncludeRoleIdsInDescriptor(descriptor), cancellationToken);
    public async Task<EntityResult<PartedResult<ProcedureReadDto>>> GetPartedAsync(ProcedureDescriptorDto descriptor, CancellationToken cancellationToken) => await Service.GetPartedAsync(await IncludeRoleIdsInDescriptor(descriptor), cancellationToken);
    public async Task<EntityResult> RemoveAsync(ProcedureWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(dto, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult> RemoveAsync(IReadOnlyList<ProcedureWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(id, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult> RemoveByDescriptorAsync(ProcedureDescriptorDto descriptor, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult<ProcedureReadDto>> UpdateAsync(ProcedureWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.UpdateAsync(dto, cancellationToken) : throw ElevatedAccessRequired;
    public async Task<EntityResult<IReadOnlyList<ProcedureReadDto>>> UpdateAsync(IReadOnlyList<ProcedureWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.UpdateAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;
}
