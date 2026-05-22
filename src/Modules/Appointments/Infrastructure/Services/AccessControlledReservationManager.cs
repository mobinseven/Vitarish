using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

internal class AccessControlledReservationManager(
    IRequestContext requestContext,
    IReservationManager reservationManager,
    IShiftBookingPolicyEngine bookingPolicyEngine,
    IStatusCoordinator statusCoordinator) : IAccessControlledReservationManager
{
    private bool ElevatedAccess => requestContext.IsPlatformAdmin() || requestContext.IsTenantOwner();

    private string? UserId => requestContext.UserId;

    private static InvalidOperationException ElevatedAccessRequired => new("Elevated access is required to perform this operation.");

    private static InvalidOperationException AccessDenied => new("Access denied: you do not own this reservation.");

    public async Task<EntityResult<ReservationReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        EntityResult<ReservationReadDto> result = await reservationManager.GetAsync(id, cancellationToken);

        return !ElevatedAccess && result.Value is not null && result.Value.ReservingPartyId != UserId
            ? EntityResult<ReservationReadDto>.Failure(EntityError.NotFound("Reservation not found.", nameof(id)))
            : result;
    }

    public Task<EntityResult<IReadOnlyList<ReservationReadDto>>> GetListAsync(ReservationDescriptorDto descriptor, CancellationToken cancellationToken)
        => reservationManager.GetListAsync(ScopeDescriptor(descriptor), cancellationToken);

    public Task<EntityResult<PartedResult<ReservationReadDto>>> GetPartedAsync(ReservationDescriptorDto descriptor, CancellationToken cancellationToken)
        => reservationManager.GetPartedAsync(ScopeDescriptor(descriptor), cancellationToken);

    public Task<EntityResult<int>> CountAsync(ReservationDescriptorDto descriptor, CancellationToken cancellationToken)
        => reservationManager.CountAsync(ScopeDescriptor(descriptor), cancellationToken);

    public Task<EntityResult<ReservationReadDto>> AddAsync(ReservationWriteDto dto, CancellationToken cancellationToken)
        => reservationManager.AddAsync(dto, cancellationToken);

    public Task<EntityResult<IReadOnlyList<ReservationReadDto>>> AddAsync(IReadOnlyList<ReservationWriteDto> dtos, CancellationToken cancellationToken)
        => ElevatedAccess ? reservationManager.AddAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;

    public async Task<EntityResult<ReservationReadDto>> UpdateAsync(ReservationWriteDto dto, CancellationToken cancellationToken)
    {
        if (!ElevatedAccess)
        {
            await EnsureOwnershipAsync(dto.Id, cancellationToken);
        }

        EntityResult<ReservationReadDto> existing = await reservationManager.GetAsync(dto.Id, cancellationToken);
        if (existing.IsSuccess && existing.Value is not null && existing.Value.Status != dto.Status)
        {
            EntityResult statusResult = await TransitionReservationAsync(dto.Id, dto.Status, cancellationToken, requireAdmin: dto.Status != ReservationStatus.Cancelled);
            if (statusResult.IsFailure)
            {
                return EntityResult<ReservationReadDto>.Failure([.. statusResult.Errors]);
            }

            return await reservationManager.GetAsync(dto.Id, cancellationToken);
        }

        return await reservationManager.UpdateAsync(dto, cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<ReservationReadDto>>> UpdateAsync(IReadOnlyList<ReservationWriteDto> dtos, CancellationToken cancellationToken)
        => ElevatedAccess ? reservationManager.UpdateAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult<ReservationReadDto>> AddOrUpdateAsync(ReservationWriteDto dto, CancellationToken cancellationToken)
        => ElevatedAccess ? reservationManager.AddOrUpdateAsync(dto, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult> RemoveAsync(ReservationWriteDto dto, CancellationToken cancellationToken)
        => ElevatedAccess ? reservationManager.RemoveAsync(dto, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult> RemoveAsync(IReadOnlyList<ReservationWriteDto> dtos, CancellationToken cancellationToken)
        => ElevatedAccess ? reservationManager.RemoveAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;

    public async Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!ElevatedAccess)
        {
            await EnsureOwnershipAsync(id, cancellationToken);
        }

        return await reservationManager.RemoveAsync(id, cancellationToken);
    }

    public Task<EntityResult> RemoveByDescriptorAsync(ReservationDescriptorDto descriptor, CancellationToken cancellationToken)
        => ElevatedAccess ? reservationManager.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw ElevatedAccessRequired;

    public async Task<EntityResult<ReservationReadDto>> ReserveAsync(
        Guid shiftId,
        DateOnly date,
        string? reservingPartyId,
        TimeSpan? estimatedSessionLength,
        CancellationToken cancellationToken)
        => await ReserveInternalAsync(shiftId, date, reservingPartyId, capacityOverride: false, estimatedSessionLength, cancellationToken);

    public async Task<EntityResult<ReservationReadDto>> ReserveWithOverrideAsync(
        Guid shiftId,
        DateOnly date,
        string? reservingPartyId,
        TimeSpan? estimatedSessionLength,
        CancellationToken cancellationToken)
        => await ReserveInternalAsync(shiftId, date, reservingPartyId, capacityOverride: true, estimatedSessionLength, cancellationToken);

    private async Task<EntityResult<ReservationReadDto>> ReserveInternalAsync(
        Guid shiftId,
        DateOnly date,
        string? reservingPartyId,
        bool capacityOverride,
        TimeSpan? estimatedSessionLength,
        CancellationToken cancellationToken)
    {
        if (capacityOverride && !ElevatedAccess)
        {
            throw ElevatedAccessRequired;
        }

        if (estimatedSessionLength.HasValue && !ElevatedAccess)
        {
            throw ElevatedAccessRequired;
        }

        string? effectivePartyId = ElevatedAccess ? reservingPartyId : UserId;
        if (string.IsNullOrWhiteSpace(effectivePartyId))
        {
            return EntityResult<ReservationReadDto>.Failure(EntityError.Validation("A reserving party is required.", new Dictionary<string, string>
            {
                [nameof(reservingPartyId)] = "A reserving party is required."
            }));
        }

        if (!await bookingPolicyEngine.IsBookingOpenAsync(shiftId, date, cancellationToken))
        {
            return EntityResult<ReservationReadDto>.Failure(EntityError.Conflict("Booking is not open for this shift slot."));
        }

        if (!capacityOverride &&
            !await bookingPolicyEngine.HasCapacityForPartyAsync(shiftId, date, effectivePartyId, cancellationToken))
        {
            return EntityResult<ReservationReadDto>.Failure(EntityError.Conflict("The reserving party has reached the reservation limit for this shift slot."));
        }

        ReservationDescriptorDto descriptor = new()
        {
            ShiftId = shiftId,
            DateFrom = date,
            DateTo = date,
            ReservingPartyId = effectivePartyId,
            Skip = 0,
            Take = int.MaxValue
        };

        EntityResult<IReadOnlyList<ReservationReadDto>> existingReservations = await reservationManager.GetListAsync(descriptor, cancellationToken);

        if (existingReservations.Value?.Any(IsActiveReservation) == true)
        {
            return EntityResult<ReservationReadDto>.Failure(EntityError.Conflict("An active reservation already exists for this shift slot."));
        }

        EntityResult<ReservationReadDto> reserveResult = capacityOverride
            ? await reservationManager.ReserveWithOverrideAsync(
                shiftId,
                date,
                effectivePartyId,
                estimatedSessionLength,
                cancellationToken)
            : await reservationManager.ReserveAsync(
                shiftId,
                date,
                effectivePartyId,
                estimatedSessionLength,
                cancellationToken);
        if (reserveResult.IsFailure || reserveResult.Value is null)
        {
            return reserveResult.IsFailure
                ? reserveResult
                : EntityResult<ReservationReadDto>.Failure(EntityError.Unknown("Reservation was not returned after creation."));
        }

        bool requiresApproval = await bookingPolicyEngine.RequiresApprovalAsync(shiftId, date, cancellationToken);

        if (!requiresApproval)
        {
            EntityResult confirmResult = await statusCoordinator.TransitionReservationAsync(
                reserveResult.Value.Id,
                ReservationStatus.Confirmed,
                requestContext.UserId,
                cancellationToken);

            if (confirmResult.IsFailure)
            {
                return EntityResult<ReservationReadDto>.Failure([.. confirmResult.Errors]);
            }
        }

        return await reservationManager.GetAsync(reserveResult.Value.Id, cancellationToken);
    }

    public Task<EntityResult> ConfirmReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => TransitionReservationAsync(reservationId, ReservationStatus.Confirmed, cancellationToken, requireAdmin: true);

    public Task<EntityResult> PresentReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => TransitionReservationAsync(reservationId, ReservationStatus.Present, cancellationToken, requireAdmin: true);

    public Task<EntityResult> CompleteReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => TransitionReservationAsync(reservationId, ReservationStatus.Completed, cancellationToken, requireAdmin: true);

    public async Task<EntityResult> CancelReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        if (!ElevatedAccess)
        {
            await EnsureOwnershipAsync(reservationId, cancellationToken);
        }

        if (!await bookingPolicyEngine.CanCancelAsync(reservationId, cancellationToken))
        {
            return EntityResult.Failure(EntityError.Conflict("This reservation can no longer be cancelled."));
        }

        return await statusCoordinator.TransitionReservationAsync(reservationId, ReservationStatus.Cancelled, requestContext.UserId, cancellationToken);
    }

    public Task<EntityResult> RejectReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => TransitionReservationAsync(reservationId, ReservationStatus.Rejected, cancellationToken, requireAdmin: true);

    public Task<EntityResult> ExpireReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => TransitionReservationAsync(reservationId, ReservationStatus.Expired, cancellationToken, requireAdmin: true);

    public Task<EntityResult> MoveReservationAsync(Guid reservationId, Guid targetShiftId, DateOnly targetDate, CancellationToken cancellationToken)
        => ElevatedAccess ? reservationManager.MoveReservationAsync(reservationId, targetShiftId, targetDate, cancellationToken) : throw ElevatedAccessRequired;

    private ReservationDescriptorDto ScopeDescriptor(ReservationDescriptorDto descriptor)
        => ElevatedAccess ? descriptor : descriptor with { ReservingPartyId = UserId };

    private async Task EnsureOwnershipAsync(Guid id, CancellationToken cancellationToken)
    {
        EntityResult<ReservationReadDto> existing = await reservationManager.GetAsync(id, cancellationToken);
        if (existing.Value?.ReservingPartyId != UserId)
        {
            throw AccessDenied;
        }
    }

    private async Task<EntityResult> TransitionReservationAsync(
        Guid reservationId,
        ReservationStatus status,
        CancellationToken cancellationToken,
        bool requireAdmin)
    {
        if (requireAdmin && !ElevatedAccess)
        {
            throw ElevatedAccessRequired;
        }

        return await statusCoordinator.TransitionReservationAsync(reservationId, status, requestContext.UserId, cancellationToken);
    }

    private static bool IsActiveReservation(ReservationReadDto reservation)
        => reservation.Status is not ReservationStatus.Cancelled
            and not ReservationStatus.Rejected
            and not ReservationStatus.Expired
            and not ReservationStatus.Completed;
}
