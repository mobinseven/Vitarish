using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Sea.Infrastructure.Services;

internal class AccessControlledEventsService(
    IRequestContext requestContext,
    IEventRolesService eventRolesService,
    IUserRolesService userRolesService,
    IEventsService eventsService) : AccessControlledServiceBase(requestContext, eventRolesService, userRolesService), IAccessControlledEventsService
{
    private IEventsService Service => eventsService;
    public async Task<EventReadDto?> AddAsync(Guid projectId, Guid procedureId, Guid? previousEventId, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddAsync(projectId, procedureId, previousEventId, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<EventReadDto>> AddAsync(EventWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddAsync(dto, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<IReadOnlyList<EventReadDto>>> AddAsync(IReadOnlyList<EventWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddAsync(dtos, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<EventReadDto>> AddOrUpdateAsync(EventWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.AddOrUpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;
    public async Task<EventSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) => await CanReadEvent(id) ? await Service.CaptureSnapshotAsync(id, cancellationToken) : throw ReadRoleRequired;
    public async Task<int> CommitAllByProcedureAsync(Guid procedureId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken) => await IsAdmin() ? await Service.CommitAllByProcedureAsync(procedureId, reporterUserId, commitTime, cancellationToken) : throw AdminRoleRequired;
    public async Task CommitAsync(Guid eventId, CancellationToken cancellationToken)
    {
        bool res = await CanWriteEvent(eventId);

        if (res)
        {
            await Service.CommitAsync(eventId, UserId, DateTimeOffset.Now, cancellationToken);
        }
        else
        {
            throw WriteRoleRequired;
        }
    }

    public Task CommitAsync(Guid eventId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<EntityResult<int>> CountAsync(EventDescriptorDto descriptor, CancellationToken cancellationToken) => Service.CountAsync(descriptor, cancellationToken);
    public async Task<EntityResult<EventReadDto>> GetAsync(Guid id, CancellationToken cancellationToken) => await CanReadEvent(id) ? await Service.GetAsync(id, cancellationToken) : throw ReadRoleRequired;
    public async Task<IReadOnlyList<EventReadDto>> GetByProcedureAsync(Guid procedureId, CancellationToken cancellationToken)
    {
        IReadOnlyList<EventReadDto>? res = await Service.GetByProcedureAsync(procedureId, cancellationToken) ?? [];
        foreach (EventReadDto? evt in res)
        {
            bool canRead = await CanReadEvent(evt.Id);
            if (!canRead)
            {
                evt.Redact();
            }
        }
        return res;
    }
    public async Task<EntityResult<IReadOnlyList<EventReadDto>>> GetListAsync(EventDescriptorDto descriptor, CancellationToken cancellationToken)  // TODO: return redacted result
    {
        IReadOnlyList<EventReadDto>? res = await Service.GetListAsync(descriptor, cancellationToken).GetValueOrDefaultAsync() ?? [];
        foreach (EventReadDto? evt in res)
        {
            bool canRead = await CanReadEvent(evt.Id);
            if (!canRead)
            {
                evt.Redact();
            }
        }
        return EntityResult<IReadOnlyList<EventReadDto>>.Success(res);
    }
    public Task<EntityResult<PartedResult<EventReadDto>>> GetPartedAsync(EventDescriptorDto descriptor, CancellationToken cancellationToken) => throw new NotImplementedException(); // TODO: return redacted result
    public async Task<int> GetRemindersCountAsync(Guid eventId, CancellationToken cancellationToken) => await IsAdmin() ? await Service.GetRemindersCountAsync(eventId, cancellationToken) : throw AdminRoleRequired;
    public async Task<bool> MoveDownAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken) => await IsAdmin() ? await Service.MoveDownAsync(procedureId, eventId, cancellationToken) : throw AdminRoleRequired;
    public async Task<bool> MoveUpAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken) => await IsAdmin() ? await Service.MoveUpAsync(procedureId, eventId, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult> RemoveAsync(EventWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(dto, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult> RemoveAsync(IReadOnlyList<EventWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(dtos, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveAsync(id, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult> RemoveByDescriptorAsync(EventDescriptorDto descriptor, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw AdminRoleRequired;
    public async Task<bool> RemoveEventAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken) => await IsAdmin() ? await Service.RemoveEventAsync(procedureId, eventId, cancellationToken) : throw AdminRoleRequired;
    public async Task RepairChainAsync(Guid procedureId, CancellationToken cancellationToken)
    {
        bool res = await IsAdmin();
        if (res)
        {
            await Service.RepairChainAsync(procedureId, cancellationToken);
        }
        else
        {
            throw AdminRoleRequired;
        }
    }
    public async Task<int> UncommitAllByProcedureAsync(Guid procedureId, CancellationToken cancellationToken) => await IsAdmin() ? await Service.UncommitAllByProcedureAsync(procedureId, cancellationToken) : throw AdminRoleRequired;
    public async Task UncommitAsync(Guid eventId, CancellationToken cancellationToken)
    {
        bool res = await CanWriteEvent(eventId);

        if (res)
        {
            await Service.UncommitAsync(eventId, cancellationToken);
        }
        else
        {
            throw WriteRoleRequired;
        }
    }
    public async Task<EventReadDto?> UpdateAsync(Guid Id, string Title, string Description, CancellationToken cancellationToken) => await IsAdmin() ? await Service.UpdateAsync(Id, Title, Description, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<EventReadDto>> UpdateAsync(EventWriteDto dto, CancellationToken cancellationToken) => await IsAdmin() ? await Service.UpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;
    public async Task<EntityResult<IReadOnlyList<EventReadDto>>> UpdateAsync(IReadOnlyList<EventWriteDto> dtos, CancellationToken cancellationToken) => await IsAdmin() ? await Service.UpdateAsync(dtos, cancellationToken) : throw AdminRoleRequired;
    public async Task UpdateChainAsync(Guid procedureId, IEnumerable<EventsChainChangeReadDto> chainChangeDtos, CancellationToken cancellationToken)
    {
        bool res = await IsAdmin();
        if (res)
        {
            await Service.UpdateChainAsync(procedureId, chainChangeDtos, cancellationToken);
        }
        else
        {
            throw AdminRoleRequired;
        }
    }

}
