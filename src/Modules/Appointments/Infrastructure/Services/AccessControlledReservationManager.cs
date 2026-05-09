using Vitarish.Entities.Server.Extensions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

internal class AccessControlledReservationManager(
    IRequestContext requestContext,
    IReservationManager reservationManager,
    IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor> occurrenceRepository,
    ISessionOccurrenceFactory occurrenceFactory,
    IBookingPolicyEngine bookingPolicyEngine,
    ICapacityEngine capacityEngine,
    IStatusCoordinator statusCoordinator) : IAccessControlledReservationManager
{
    private bool IsAdmin => requestContext.IsTenantAdmin() || requestContext.IsTenantOwner();

    private string? UserId => requestContext.UserId;

    private static InvalidOperationException AdminRoleRequired => new("Admin role is required to perform this operation.");

    private static InvalidOperationException AccessDenied => new("Access denied: you do not own this reservation.");

    public async Task<EntityResult<ReservationReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        EntityResult<ReservationReadDto> result = await reservationManager.GetAsync(id, cancellationToken);

        if (!IsAdmin && result.Value is not null && result.Value.ReservingPartyId != UserId)
        {
            return EntityResult<ReservationReadDto>.Failure(EntityError.NotFound("Reservation not found.", nameof(id)));
        }

        return result;
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
        => IsAdmin ? reservationManager.AddAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public async Task<EntityResult<ReservationReadDto>> UpdateAsync(ReservationWriteDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin)
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
        => IsAdmin ? reservationManager.UpdateAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<ReservationReadDto>> AddOrUpdateAsync(ReservationWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? reservationManager.AddOrUpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(ReservationWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? reservationManager.RemoveAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(IReadOnlyList<ReservationWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? reservationManager.RemoveAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public async Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!IsAdmin)
        {
            await EnsureOwnershipAsync(id, cancellationToken);
        }

        return await reservationManager.RemoveAsync(id, cancellationToken);
    }

    public Task<EntityResult> RemoveByDescriptorAsync(ReservationDescriptorDto descriptor, CancellationToken cancellationToken)
        => IsAdmin ? reservationManager.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw AdminRoleRequired;

    public async Task<EntityResult<ReservationReadDto>> ReserveAsync(Guid sessionId, DateOnly date, string? reservingPartyId, CancellationToken cancellationToken)
    {
        string? effectivePartyId = IsAdmin ? reservingPartyId : UserId;
        if (string.IsNullOrWhiteSpace(effectivePartyId))
        {
            return EntityResult<ReservationReadDto>.Failure(EntityError.Validation("A reserving user is required.", new Dictionary<string, string>
            {
                [nameof(reservingPartyId)] = "A reserving user is required."
            }));
        }

        EntityResult<Guid> occurrenceResult = await occurrenceFactory.GetOrCreateAsync(sessionId, date, cancellationToken);
        return occurrenceResult.IsFailure
            ? EntityResult<ReservationReadDto>.Failure([.. occurrenceResult.Errors])
            : await ReserveOccurrenceAsync(sessionId, date, occurrenceResult.Value, effectivePartyId, cancellationToken);
    }

    private async Task<EntityResult<ReservationReadDto>> ReserveOccurrenceAsync(
        Guid sessionId,
        DateOnly date,
        Guid sessionOccurrenceId,
        string reservingPartyId,
        CancellationToken cancellationToken)
    {
        SessionOccurrence? occurrence = await occurrenceRepository.GetAsync(sessionOccurrenceId, cancellationToken);
        if (occurrence?.Session is null)
        {
            return EntityResult<ReservationReadDto>.Failure(EntityError.NotFound("Session occurrence not found.", nameof(sessionOccurrenceId)));
        }

        if (occurrence.IsCancelled || occurrence.Session.Status != SessionStatus.Active)
        {
            return EntityResult<ReservationReadDto>.Failure(EntityError.Conflict("This session occurrence is not open for reservations."));
        }

        if (!await bookingPolicyEngine.IsBookingOpenAsync(sessionOccurrenceId, cancellationToken))
        {
            return EntityResult<ReservationReadDto>.Failure(EntityError.Conflict("Booking is not open for this session occurrence."));
        }

        if (!await bookingPolicyEngine.HasCapacityForPartyAsync(sessionOccurrenceId, reservingPartyId, cancellationToken))
        {
            return EntityResult<ReservationReadDto>.Failure(EntityError.Conflict("The reserving party has reached the reservation limit for this session occurrence."));
        }

        if (!await capacityEngine.IsAvailableAsync(sessionOccurrenceId, cancellationToken))
        {
            return EntityResult<ReservationReadDto>.Failure(EntityError.Conflict("No seats are available for this session occurrence."));
        }

        ReservationDescriptorDto descriptor = new()
        {
            SessionOccurrenceId = sessionOccurrenceId,
            SessionId = sessionId,
            Date = date,
            ReservingPartyId = reservingPartyId,
            Skip = 0,
            Take = int.MaxValue
        };

        EntityResult<IReadOnlyList<ReservationReadDto>> existingReservations = await reservationManager.GetListAsync(descriptor, cancellationToken);

        if (existingReservations.Value?.Any(IsActiveReservation) == true)
        {
            return EntityResult<ReservationReadDto>.Failure(EntityError.Conflict("An active reservation already exists for this session occurrence."));
        }

        EntityResult<ReservationReadDto> reserveResult = await reservationManager.ReserveAsync(sessionId, date, reservingPartyId, cancellationToken);
        if (reserveResult.IsFailure || reserveResult.Value is null)
        {
            return reserveResult.IsFailure
                ? reserveResult
                : EntityResult<ReservationReadDto>.Failure(EntityError.Unknown("Reservation was not returned after creation."));
        }

        if (occurrence.Session.RequiresApproval)
        {
            return reserveResult;
        }

        EntityResult confirmResult = await statusCoordinator.TransitionReservationAsync(
            reserveResult.Value.Id,
            ReservationStatus.Confirmed,
            requestContext.UserId,
            cancellationToken);

        if (confirmResult.IsFailure)
        {
            return EntityResult<ReservationReadDto>.Failure([.. confirmResult.Errors]);
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
        if (!IsAdmin)
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

    public Task<EntityResult> MoveReservationAsync(Guid reservationId, Guid newSessionOccurrenceId, CancellationToken cancellationToken)
        => IsAdmin ? reservationManager.MoveReservationAsync(reservationId, newSessionOccurrenceId, cancellationToken) : throw AdminRoleRequired;

    private ReservationDescriptorDto ScopeDescriptor(ReservationDescriptorDto descriptor)
        => IsAdmin ? descriptor : descriptor with { ReservingPartyId = UserId };

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
        if (requireAdmin && !IsAdmin)
        {
            throw AdminRoleRequired;
        }

        return await statusCoordinator.TransitionReservationAsync(reservationId, status, requestContext.UserId, cancellationToken);
    }

    private static bool IsActiveReservation(ReservationReadDto reservation)
        => reservation.Status is not ReservationStatus.Cancelled
            and not ReservationStatus.Rejected
            and not ReservationStatus.Expired
            and not ReservationStatus.Completed;
}
