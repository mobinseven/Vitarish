using Vitarish.Entities.Server.Extensions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

internal class AccessControlledShiftManager(
    IRequestContext requestContext,
    IShiftManager shiftManager,
    IStatusCoordinator statusCoordinator) : IAccessControlledShiftManager
{
    private bool IsAdmin => requestContext.IsTenantAdmin() || requestContext.IsTenantOwner();

    private static InvalidOperationException AdminRoleRequired => new("Admin role is required to perform this operation.");

    public async Task<EntityResult<ShiftReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        EntityResult<ShiftReadDto> result = await shiftManager.GetAsync(id, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? FilterSingle(result.Value)
            : result;
    }

    public async Task<EntityResult<IReadOnlyList<ShiftReadDto>>> GetListAsync(ShiftDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        EntityResult<IReadOnlyList<ShiftReadDto>> result = await shiftManager.GetListAsync(descriptor, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? EntityResult<IReadOnlyList<ShiftReadDto>>.Success(FilterList(result.Value))
            : result;
    }

    public async Task<EntityResult<PartedResult<ShiftReadDto>>> GetPartedAsync(ShiftDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        EntityResult<PartedResult<ShiftReadDto>> result = await shiftManager.GetPartedAsync(descriptor, cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            return result;
        }

        IReadOnlyList<ShiftReadDto> filtered = FilterList(result.Value.Items);
        return EntityResult<PartedResult<ShiftReadDto>>.Success(new PartedResult<ShiftReadDto>(filtered, filtered.Count));
    }

    public async Task<EntityResult<int>> CountAsync(ShiftDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        EntityResult<IReadOnlyList<ShiftReadDto>> listResult = await GetListAsync(descriptor, cancellationToken);
        return listResult.IsSuccess && listResult.Value is not null
            ? EntityResult<int>.Success(listResult.Value.Count)
            : EntityResult<int>.Failure([.. listResult.Errors]);
    }

    public async Task<EntityResult<ShiftReadDto>> AddAsync(ShiftWriteDto dto, CancellationToken cancellationToken)
    {
        EnsureAdmin();

        EntityResult validation = ValidateSchedule(dto);
        if (validation.IsFailure)
        {
            return EntityResult<ShiftReadDto>.Failure([.. validation.Errors]);
        }

        return await shiftManager.AddAsync(dto, cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<ShiftReadDto>>> AddAsync(IReadOnlyList<ShiftWriteDto> dtos, CancellationToken cancellationToken)
        => throw new NotSupportedException("Batch shift writes are not supported for scheduled shifts.");

    public async Task<EntityResult<ShiftReadDto>> UpdateAsync(ShiftWriteDto dto, CancellationToken cancellationToken)
    {
        EnsureAdmin();

        EntityResult validation = ValidateSchedule(dto);
        if (validation.IsFailure)
        {
            return EntityResult<ShiftReadDto>.Failure([.. validation.Errors]);
        }

        return await shiftManager.UpdateAsync(dto, cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<ShiftReadDto>>> UpdateAsync(IReadOnlyList<ShiftWriteDto> dtos, CancellationToken cancellationToken)
        => throw new NotSupportedException("Batch shift writes are not supported for scheduled shifts.");

    public async Task<EntityResult<ShiftReadDto>> AddOrUpdateAsync(ShiftWriteDto dto, CancellationToken cancellationToken)
        => dto.Id == Guid.Empty
            ? await AddAsync(dto, cancellationToken)
            : await UpdateAsync(dto, cancellationToken);

    public Task<EntityResult> RemoveAsync(ShiftWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? shiftManager.RemoveAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(IReadOnlyList<ShiftWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? shiftManager.RemoveAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
        => IsAdmin ? shiftManager.RemoveAsync(id, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveByDescriptorAsync(ShiftDescriptorDto descriptor, CancellationToken cancellationToken)
        => IsAdmin ? shiftManager.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw AdminRoleRequired;

    public async Task<EntityResult> ActivateShiftAsync(Guid shiftId, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        EntityResult<ShiftReadDto> shift = await shiftManager.GetAsync(shiftId, cancellationToken);
        if (shift.IsFailure || shift.Value is null)
        {
            return EntityResult.Failure([.. shift.Errors]);
        }

        EntityResult validation = ValidateSchedule(shift.Value.ToWrite());
        if (validation.IsFailure)
        {
            return validation;
        }

        return await statusCoordinator.TransitionShiftAsync(shiftId, ShiftStatus.Active, requestContext.UserId, cancellationToken);
    }

    public Task<EntityResult> PauseShiftAsync(Guid shiftId, CancellationToken cancellationToken)
        => TransitionShiftAsync(shiftId, ShiftStatus.Paused, cancellationToken);

    public Task<EntityResult> CloseShiftAsync(Guid shiftId, CancellationToken cancellationToken)
        => TransitionShiftAsync(shiftId, ShiftStatus.Closed, cancellationToken);

    public Task<EntityResult> CancelShiftAsync(Guid shiftId, CancellationToken cancellationToken)
        => TransitionShiftAsync(shiftId, ShiftStatus.Cancelled, cancellationToken);

    public Task<EntityResult> ArchiveShiftAsync(Guid shiftId, CancellationToken cancellationToken)
        => TransitionShiftAsync(shiftId, ShiftStatus.Archived, cancellationToken);

    private async Task<EntityResult> TransitionShiftAsync(Guid shiftId, ShiftStatus status, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        return await statusCoordinator.TransitionShiftAsync(shiftId, status, requestContext.UserId, cancellationToken);
    }

    private IReadOnlyList<ShiftReadDto> FilterList(IReadOnlyList<ShiftReadDto> shifts)
        => IsAdmin
            ? shifts
            : [.. shifts.Where(IsVisibleToTenantUser)];

    private EntityResult<ShiftReadDto> FilterSingle(ShiftReadDto shift)
        => IsAdmin || IsVisibleToTenantUser(shift)
            ? EntityResult<ShiftReadDto>.Success(shift)
            : EntityResult<ShiftReadDto>.Failure(EntityError.NotFound("Shift not found.", nameof(shift.Id)));

    private static bool IsVisibleToTenantUser(ShiftReadDto shift)
        => shift.Status == ShiftStatus.Active;

    private static EntityResult ValidateSchedule(ShiftWriteDto dto)
    {
        if (dto.StartTime >= dto.EndTime)
        {
            return EntityResult.Failure(EntityError.Validation("Shift end time must be later than start time.", new Dictionary<string, string>
            {
                [nameof(dto.EndTime)] = "End time must be later than start time."
            }));
        }

        if (dto.TotalSeats <= 0)
        {
            return EntityResult.Failure(EntityError.Validation("Shift capacity must be greater than zero.", new Dictionary<string, string>
            {
                [nameof(dto.TotalSeats)] = "Capacity must be greater than zero."
            }));
        }

        if (string.IsNullOrWhiteSpace(dto.TimeZone))
        {
            return EntityResult.Failure(EntityError.Validation("Shift time zone is required.", new Dictionary<string, string>
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
            return EntityResult.Failure(EntityError.Validation("Shift time zone is invalid.", new Dictionary<string, string>
            {
                [nameof(dto.TimeZone)] = "Time zone is invalid."
            }));
        }
        catch (InvalidTimeZoneException)
        {
            return EntityResult.Failure(EntityError.Validation("Shift time zone is invalid.", new Dictionary<string, string>
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
