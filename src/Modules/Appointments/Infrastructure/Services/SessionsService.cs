namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionsService(IMediator mediator)
    : EntityService<Session, SessionDescriptor, SessionReadDto, SessionWriteDto, SessionDescriptorDto>(
        mediator,
        session => session.ToDto(),
        session => session.ToDomain(),
        session => session.ToDomain()),
      ISessionsService
{
    public async Task<EntityResult<int>> GetAvailableCapacityAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        EntityResult<SessionReadDto> result = await ((IEntityService<SessionReadDto, SessionWriteDto, SessionDescriptorDto>)this)
            .GetAsync(sessionId, cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return EntityResult<int>.Failure(EntityError.NotFound("Session not found.", nameof(sessionId)));
        }

        return EntityResult<int>.Success(result.Value.Capacity - result.Value.ReservationCount);
    }
}
