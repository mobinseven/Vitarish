using Vitarish.Entities.Server.Extensions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

internal class AccessControlledAppointmentsService(
    IRequestContext requestContext,
    IAppointmentsService appointmentsService) : IAccessControlledAppointmentsService
{
    private bool IsAdmin => requestContext.IsTenantAdmin() || requestContext.IsTenantOwner();

    private string? UserId => requestContext.UserId;

    private static InvalidOperationException AdminRoleRequired => new("Admin role is required to perform this operation.");

    private static InvalidOperationException AccessDenied => new("Access denied: you do not own this appointment.");

    // ── Reads ──────────────────────────────────────────────────────────────────

    public async Task<EntityResult<AppointmentReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        EntityResult<AppointmentReadDto> result = await appointmentsService.GetAsync(id, cancellationToken);

        if (!IsAdmin && result.Value is not null && result.Value.CreatedBy != UserId)
        {
            return EntityResult<AppointmentReadDto>.Failure(EntityError.NotFound("Appointment not found.", nameof(id)));
        }

        return result;
    }

    public Task<EntityResult<IReadOnlyList<AppointmentReadDto>>> GetListAsync(AppointmentDescriptorDto descriptor, CancellationToken cancellationToken)
        => appointmentsService.GetListAsync(ScopeDescriptor(descriptor), cancellationToken);

    public Task<EntityResult<PartedResult<AppointmentReadDto>>> GetPartedAsync(AppointmentDescriptorDto descriptor, CancellationToken cancellationToken)
        => appointmentsService.GetPartedAsync(ScopeDescriptor(descriptor), cancellationToken);

    public Task<EntityResult<int>> CountAsync(AppointmentDescriptorDto descriptor, CancellationToken cancellationToken)
        => appointmentsService.CountAsync(ScopeDescriptor(descriptor), cancellationToken);

    // ── Writes ─────────────────────────────────────────────────────────────────

    public Task<EntityResult<AppointmentReadDto>> AddAsync(AppointmentWriteDto dto, CancellationToken cancellationToken)
        => appointmentsService.AddAsync(dto, cancellationToken);

    public Task<EntityResult<IReadOnlyList<AppointmentReadDto>>> AddAsync(IReadOnlyList<AppointmentWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.AddAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public async Task<EntityResult<AppointmentReadDto>> UpdateAsync(AppointmentWriteDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin)
        {
            await EnsureOwnershipAsync(dto.Id, cancellationToken);
        }

        return await appointmentsService.UpdateAsync(dto, cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<AppointmentReadDto>>> UpdateAsync(IReadOnlyList<AppointmentWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.UpdateAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<AppointmentReadDto>> AddOrUpdateAsync(AppointmentWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.AddOrUpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(AppointmentWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.RemoveAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(IReadOnlyList<AppointmentWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.RemoveAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public async Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!IsAdmin)
        {
            await EnsureOwnershipAsync(id, cancellationToken);
        }

        return await appointmentsService.RemoveAsync(id, cancellationToken);
    }

    public Task<EntityResult> RemoveByDescriptorAsync(AppointmentDescriptorDto descriptor, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw AdminRoleRequired;

    public async Task<EntityResult> UpdateStatusAsync(Guid id, AppointmentStatus status, CancellationToken cancellationToken)
    {
        if (!IsAdmin)
        {
            if (status != AppointmentStatus.Cancelled)
            {
                throw AdminRoleRequired;
            }

            await EnsureOwnershipAsync(id, cancellationToken);
        }

        return await appointmentsService.UpdateStatusAsync(id, status, cancellationToken);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// For non-admin users, restricts the descriptor so that only the current user's
    /// appointments are visible, by setting <c>CreatedBy</c> to the current user ID.
    /// </summary>
    private AppointmentDescriptorDto ScopeDescriptor(AppointmentDescriptorDto descriptor)
        => IsAdmin ? descriptor : descriptor with { CreatedBy = UserId };

    /// <summary>
    /// Fetches the appointment by <paramref name="id"/> and throws <see cref="AccessDenied"/>
    /// if the current user is not its creator.
    /// </summary>
    private async Task EnsureOwnershipAsync(Guid id, CancellationToken cancellationToken)
    {
        EntityResult<AppointmentReadDto> existing = await appointmentsService.GetAsync(id, cancellationToken);
        if (existing.Value?.CreatedBy != UserId)
        {
            throw AccessDenied;
        }
    }
}
