using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

internal class AccessControlledAppointmentSessionsService(
    IRequestContext requestContext,
    IAppointmentSessionsService sessionsService) : IAccessControlledAppointmentSessionsService
{
    private bool IsAdmin => requestContext.IsTenantAdmin() || requestContext.IsTenantOwner();

    private static InvalidOperationException AdminRoleRequired => new("Admin role is required to perform this operation.");

    // ── Reads (public — any authenticated user can browse sessions) ────────────

    public Task<EntityResult<AppointmentSessionReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
        => sessionsService.GetAsync(id, cancellationToken);

    public Task<EntityResult<IReadOnlyList<AppointmentSessionReadDto>>> GetListAsync(AppointmentSessionDescriptorDto descriptor, CancellationToken cancellationToken)
        => sessionsService.GetListAsync(descriptor, cancellationToken);

    public Task<EntityResult<PartedResult<AppointmentSessionReadDto>>> GetPartedAsync(AppointmentSessionDescriptorDto descriptor, CancellationToken cancellationToken)
        => sessionsService.GetPartedAsync(descriptor, cancellationToken);

    public Task<EntityResult<int>> CountAsync(AppointmentSessionDescriptorDto descriptor, CancellationToken cancellationToken)
        => sessionsService.CountAsync(descriptor, cancellationToken);

    // ── Reservation (any authenticated user) ─────────────────────────────────

    public Task<EntityResult> ReserveAsync(Guid sessionId, CancellationToken cancellationToken)
        => sessionsService.ReserveAsync(sessionId, cancellationToken);

    public Task<EntityResult> ReserveForUserAsync(Guid sessionId, string? userId, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.ReserveForUserAsync(sessionId, userId, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> CancelReservationAsync(Guid sessionId, CancellationToken cancellationToken)
        => sessionsService.CancelReservationAsync(sessionId, cancellationToken);

    // ── Writes (admin only) ────────────────────────────────────────────────────

    public Task<EntityResult<AppointmentSessionReadDto>> AddAsync(AppointmentSessionWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.AddAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<IReadOnlyList<AppointmentSessionReadDto>>> AddAsync(IReadOnlyList<AppointmentSessionWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.AddAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<AppointmentSessionReadDto>> UpdateAsync(AppointmentSessionWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.UpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<IReadOnlyList<AppointmentSessionReadDto>>> UpdateAsync(IReadOnlyList<AppointmentSessionWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.UpdateAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<AppointmentSessionReadDto>> AddOrUpdateAsync(AppointmentSessionWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.AddOrUpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(AppointmentSessionWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.RemoveAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(IReadOnlyList<AppointmentSessionWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.RemoveAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.RemoveAsync(id, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveByDescriptorAsync(AppointmentSessionDescriptorDto descriptor, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw AdminRoleRequired;
}
