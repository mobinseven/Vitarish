using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Appointments.Application.Abstractions;

public interface ISessionsService : IEntityService<AppointmentSessionReadDto, AppointmentSessionWriteDto, AppointmentSessionDescriptorDto>
{
    Task<EntityResult> ReserveAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<EntityResult> CancelReservationAsync(Guid sessionId, CancellationToken cancellationToken);
}
