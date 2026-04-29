using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Appointments.Application.Abstractions;

public interface ISessionsService : IEntityService<SessionReadDto, SessionWriteDto, SessionDescriptorDto>
{
    Task<EntityResult<int>> GetAvailableCapacityAsync(Guid sessionId, CancellationToken cancellationToken);

}
