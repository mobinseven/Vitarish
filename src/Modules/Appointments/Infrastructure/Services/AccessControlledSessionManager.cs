using Vitarish.Entities.Server.Extensions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

internal class AccessControlledSessionManager(
    IRequestContext requestContext,
    ISessionManager sessionManager,
    ISessionOccurrenceSyncQueue sessionOccurrenceSyncQueue,
    IStatusCoordinator statusCoordinator) : IAccessControlledSessionManager
{
    private bool IsAdmin => requestContext.IsTenantAdmin() || requestContext.IsTenantOwner();

    private static InvalidOperationException AdminRoleRequired => new("Admin role is required to perform this operation.");

    public async Task<EntityResult<SessionReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        EntityResult<SessionReadDto> result = await sessionManager.GetAsync(id, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? FilterSingle(result.Value)
            : result;
    }

    public async Task<EntityResult<IReadOnlyList<SessionReadDto>>> GetListAsync(SessionDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        EntityResult<IReadOnlyList<SessionReadDto>> result = await sessionManager.GetListAsync(descriptor, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? EntityResult<IReadOnlyList<SessionReadDto>>.Success(FilterList(result.Value))
            : result;
    }

    public async Task<EntityResult<PartedResult<SessionReadDto>>> GetPartedAsync(SessionDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        EntityResult<PartedResult<SessionReadDto>> result = await sessionManager.GetPartedAsync(descriptor, cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            return result;
        }

        IReadOnlyList<SessionReadDto> filtered = FilterList(result.Value.Items);
        return EntityResult<PartedResult<SessionReadDto>>.Success(new PartedResult<SessionReadDto>(filtered, filtered.Count));
    }

    public async Task<EntityResult<int>> CountAsync(SessionDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        EntityResult<IReadOnlyList<SessionReadDto>> listResult = await GetListAsync(descriptor, cancellationToken);
        return listResult.IsSuccess && listResult.Value is not null
            ? EntityResult<int>.Success(listResult.Value.Count)
            : EntityResult<int>.Failure([.. listResult.Errors]);
    }

    public async Task<EntityResult<SessionReadDto>> AddAsync(SessionWriteDto dto, CancellationToken cancellationToken)
    {
        EnsureAdmin();

        EntityResult validation = ValidateSchedule(dto);
        if (validation.IsFailure)
        {
            return EntityResult<SessionReadDto>.Failure([.. validation.Errors]);
        }

        EntityResult<SessionReadDto> result = await sessionManager.AddAsync(dto, cancellationToken);
        return SyncOccurrencesAfterWrite(result);
    }

    public Task<EntityResult<IReadOnlyList<SessionReadDto>>> AddAsync(IReadOnlyList<SessionWriteDto> dtos, CancellationToken cancellationToken)
        => throw new NotSupportedException("Batch session writes are not supported for scheduled sessions.");

    public async Task<EntityResult<SessionReadDto>> UpdateAsync(SessionWriteDto dto, CancellationToken cancellationToken)
    {
        EnsureAdmin();

        EntityResult validation = ValidateSchedule(dto);
        if (validation.IsFailure)
        {
            return EntityResult<SessionReadDto>.Failure([.. validation.Errors]);
        }

        EntityResult<SessionReadDto> result = await sessionManager.UpdateAsync(dto, cancellationToken);
        return SyncOccurrencesAfterWrite(result);
    }

    public Task<EntityResult<IReadOnlyList<SessionReadDto>>> UpdateAsync(IReadOnlyList<SessionWriteDto> dtos, CancellationToken cancellationToken)
        => throw new NotSupportedException("Batch session writes are not supported for scheduled sessions.");

    public async Task<EntityResult<SessionReadDto>> AddOrUpdateAsync(SessionWriteDto dto, CancellationToken cancellationToken)
        => dto.Id == Guid.Empty
            ? await AddAsync(dto, cancellationToken)
            : await UpdateAsync(dto, cancellationToken);

    public Task<EntityResult> RemoveAsync(SessionWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.RemoveAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(IReadOnlyList<SessionWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.RemoveAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.RemoveAsync(id, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveByDescriptorAsync(SessionDescriptorDto descriptor, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw AdminRoleRequired;

    public async Task<EntityResult> ActivateSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        EntityResult<SessionReadDto> session = await sessionManager.GetAsync(sessionId, cancellationToken);
        if (session.IsFailure || session.Value is null)
        {
            return EntityResult.Failure([.. session.Errors]);
        }

        EntityResult validation = ValidateSchedule(session.Value.ToWrite());
        if (validation.IsFailure)
        {
            return validation;
        }

        sessionOccurrenceSyncQueue.EnqueueSync(sessionId);
        return await statusCoordinator.TransitionSessionAsync(sessionId, SessionStatus.Active, requestContext.UserId, cancellationToken);
    }

    public Task<EntityResult> PauseSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => TransitionSessionAsync(sessionId, SessionStatus.Paused, cancellationToken);

    public Task<EntityResult> CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => TransitionSessionAsync(sessionId, SessionStatus.Closed, cancellationToken);

    public Task<EntityResult> CancelSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => TransitionSessionAsync(sessionId, SessionStatus.Cancelled, cancellationToken);

    public Task<EntityResult> ArchiveSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => TransitionSessionAsync(sessionId, SessionStatus.Archived, cancellationToken);

    private async Task<EntityResult> TransitionSessionAsync(Guid sessionId, SessionStatus status, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        return await statusCoordinator.TransitionSessionAsync(sessionId, status, requestContext.UserId, cancellationToken);
    }

    private EntityResult<SessionReadDto> SyncOccurrencesAfterWrite(EntityResult<SessionReadDto> result)
    {
        if (result.IsFailure || result.Value is null)
        {
            return result;
        }

        sessionOccurrenceSyncQueue.EnqueueSync(result.Value.Id);
        return result;
    }

    private IReadOnlyList<SessionReadDto> FilterList(IReadOnlyList<SessionReadDto> sessions)
        => IsAdmin
            ? sessions
            : [.. sessions.Where(IsVisibleToTenantUser)];

    private EntityResult<SessionReadDto> FilterSingle(SessionReadDto session)
        => IsAdmin || IsVisibleToTenantUser(session)
            ? EntityResult<SessionReadDto>.Success(session)
            : EntityResult<SessionReadDto>.Failure(EntityError.NotFound("Session not found.", nameof(session.Id)));

    private static bool IsVisibleToTenantUser(SessionReadDto session)
        => session.Status == SessionStatus.Active
            && session.StartAt.HasValue
            && session.EndAt.HasValue
            && session.EndAt > DateTimeOffset.UtcNow;

    private static EntityResult ValidateSchedule(SessionWriteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return EntityResult.Failure(EntityError.Validation("Session title is required.", new Dictionary<string, string>
            {
                [nameof(dto.Title)] = "Title is required."
            }));
        }

        if (dto.StartAt is null || dto.EndAt is null)
        {
            return EntityResult.Failure(EntityError.Validation("Session start and end time are required.", new Dictionary<string, string>
            {
                [nameof(dto.StartAt)] = "Start time is required.",
                [nameof(dto.EndAt)] = "End time is required."
            }));
        }

        if (dto.EndAt <= dto.StartAt)
        {
            return EntityResult.Failure(EntityError.Validation("Session end time must be later than start time.", new Dictionary<string, string>
            {
                [nameof(dto.EndAt)] = "End time must be later than start time."
            }));
        }

        if (dto.TotalSeats <= 0)
        {
            return EntityResult.Failure(EntityError.Validation("Session capacity must be greater than zero.", new Dictionary<string, string>
            {
                [nameof(dto.TotalSeats)] = "Capacity must be greater than zero."
            }));
        }

        if (string.IsNullOrWhiteSpace(dto.TimeZone))
        {
            return EntityResult.Failure(EntityError.Validation("Session time zone is required.", new Dictionary<string, string>
            {
                [nameof(dto.TimeZone)] = "Time zone is required."
            }));
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(dto.TimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return EntityResult.Failure(EntityError.Validation("Session time zone is invalid.", new Dictionary<string, string>
            {
                [nameof(dto.TimeZone)] = "Time zone is invalid."
            }));
        }
        catch (InvalidTimeZoneException)
        {
            return EntityResult.Failure(EntityError.Validation("Session time zone is invalid.", new Dictionary<string, string>
            {
                [nameof(dto.TimeZone)] = "Time zone is invalid."
            }));
        }

        return EntityResult.Success();
    }

    private void EnsureAdmin()
    {
        if (!IsAdmin)
        {
            throw AdminRoleRequired;
        }
    }
}
