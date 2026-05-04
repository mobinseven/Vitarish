using Vitarish.Entities.Server.Models;

namespace Vitarish.Appointments.Application.Abstractions;

public interface ISessionManager : IEntityService<SessionReadDto, SessionWriteDto, SessionDescriptorDto>
{
    Task<EntityResult> ActivateSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<EntityResult> PauseSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<EntityResult> CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<EntityResult> CancelSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<EntityResult> ArchiveSessionAsync(Guid sessionId, CancellationToken cancellationToken);
}
