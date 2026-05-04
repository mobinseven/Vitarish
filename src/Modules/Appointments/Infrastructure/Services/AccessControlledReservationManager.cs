using Vitarish.Entities.Server.Extensions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

internal class AccessControlledReservationManager(
    IRequestContext requestContext,
    IReservationManager reservationManager) : IAccessControlledReservationManager
{
    private bool IsAdmin => requestContext.IsTenantAdmin() || requestContext.IsTenantOwner();

    private string? UserId => requestContext.UserId;

    private static InvalidOperationException AdminRoleRequired => new("Admin role is required to perform this operation.");

    private static InvalidOperationException AccessDenied => new("Access denied: you do not own this reservation.");

    // ── Reads ──────────────────────────────────────────────────────────────────

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

    // ── Writes ─────────────────────────────────────────────────────────────────

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

    // ── Reservation actions ────────────────────────────────────────────────────

    public Task<EntityResult> ReserveAsync(Guid sessionOccurrenceId, string? reservingPartyId, CancellationToken cancellationToken)
        => reservationManager.ReserveAsync(sessionOccurrenceId, reservingPartyId, cancellationToken);

    public Task<EntityResult> ConfirmReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => IsAdmin ? reservationManager.ConfirmReservationAsync(reservationId, cancellationToken) : throw AdminRoleRequired;

    public async Task<EntityResult> CancelReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        if (!IsAdmin)
        {
            await EnsureOwnershipAsync(reservationId, cancellationToken);
        }

        return await reservationManager.CancelReservationAsync(reservationId, cancellationToken);
    }

    public Task<EntityResult> RejectReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => IsAdmin ? reservationManager.RejectReservationAsync(reservationId, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> ExpireReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => IsAdmin ? reservationManager.ExpireReservationAsync(reservationId, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> MoveReservationAsync(Guid reservationId, Guid newSessionOccurrenceId, CancellationToken cancellationToken)
        => IsAdmin ? reservationManager.MoveReservationAsync(reservationId, newSessionOccurrenceId, cancellationToken) : throw AdminRoleRequired;

    // ── Helpers ────────────────────────────────────────────────────────────────

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
}
