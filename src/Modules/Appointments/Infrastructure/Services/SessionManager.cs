namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionManager(IMediator mediator)
    : EntityService<Session, SessionDescriptor, SessionReadDto, SessionWriteDto, SessionDescriptorDto>(
        mediator,
        session => session.ToDto(),
        session => session.ToDomain(),
        session => session.ToDomain()),
      ISessionManager
{
    public Task<EntityResult> ActivateSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => UpdateStatusAsync(sessionId, SessionStatus.Active, cancellationToken);

    public Task<EntityResult> PauseSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => UpdateStatusAsync(sessionId, SessionStatus.Paused, cancellationToken);

    public Task<EntityResult> CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => UpdateStatusAsync(sessionId, SessionStatus.Closed, cancellationToken);

    public Task<EntityResult> CancelSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => UpdateStatusAsync(sessionId, SessionStatus.Cancelled, cancellationToken);

    public Task<EntityResult> ArchiveSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => UpdateStatusAsync(sessionId, SessionStatus.Archived, cancellationToken);

    private async Task<EntityResult> UpdateStatusAsync(Guid sessionId, SessionStatus status, CancellationToken cancellationToken)
    {
        await Mediator.Send(new UpdateSessionStatusCommand(sessionId, status), cancellationToken);
        return EntityResult.Success();
    }
}
