using Vitarish.Entities.Server.Extensions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

internal class AccessControlledAppointmentsService(
    IRequestContext requestContext,
    IAppointmentsService appointmentsService) : IAccessControlledAppointmentsService
{
    private bool IsAdmin => requestContext.IsTenantAdmin() || requestContext.IsTenantOwner();

    private static Exception AdminRoleRequired => new InvalidOperationException("Admin role is required to perform this operation.");

    public Task<EntityResult<AppointmentReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
        => appointmentsService.GetAsync(id, cancellationToken);

    public Task<EntityResult<IReadOnlyList<AppointmentReadDto>>> GetListAsync(AppointmentDescriptorDto descriptor, CancellationToken cancellationToken)
        => appointmentsService.GetListAsync(descriptor, cancellationToken);

    public Task<EntityResult<PartedResult<AppointmentReadDto>>> GetPartedAsync(AppointmentDescriptorDto descriptor, CancellationToken cancellationToken)
        => appointmentsService.GetPartedAsync(descriptor, cancellationToken);

    public Task<EntityResult<int>> CountAsync(AppointmentDescriptorDto descriptor, CancellationToken cancellationToken)
        => appointmentsService.CountAsync(descriptor, cancellationToken);

    public Task<EntityResult<AppointmentReadDto>> AddAsync(AppointmentWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.AddAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<IReadOnlyList<AppointmentReadDto>>> AddAsync(IReadOnlyList<AppointmentWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.AddAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<AppointmentReadDto>> UpdateAsync(AppointmentWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.UpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<IReadOnlyList<AppointmentReadDto>>> UpdateAsync(IReadOnlyList<AppointmentWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.UpdateAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<AppointmentReadDto>> AddOrUpdateAsync(AppointmentWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.AddOrUpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(AppointmentWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.RemoveAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(IReadOnlyList<AppointmentWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.RemoveAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.RemoveAsync(id, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveByDescriptorAsync(AppointmentDescriptorDto descriptor, CancellationToken cancellationToken)
        => IsAdmin ? appointmentsService.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw AdminRoleRequired;
}
