using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Appointments.Application.Abstractions;

public interface IAppointmentSessionsService : IEntityService<AppointmentSessionReadDto, AppointmentSessionWriteDto, AppointmentSessionDescriptorDto>
{
    Task<EntityResult> ReserveAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<EntityResult> ReserveForUserAsync(Guid sessionId, string? userId, CancellationToken cancellationToken);
    Task<EntityResult> CancelReservationAsync(Guid sessionId, CancellationToken cancellationToken);
}
