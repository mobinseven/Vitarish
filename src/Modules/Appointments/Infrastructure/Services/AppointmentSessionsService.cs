using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class AppointmentSessionsService(IMediator mediator, IRequestContext requestContext)
    : EntityService<AppointmentSession, AppointmentSessionDescriptor, AppointmentSessionReadDto, AppointmentSessionWriteDto, AppointmentSessionDescriptorDto>(
        mediator,
        session => session.ToDto(),
        session => session.ToDomain(),
        session => session.ToDomain()),
      IAppointmentSessionsService
{
    public async Task<EntityResult> ReserveAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        string? userId = requestContext.UserId;
        return await ReserveForUserAsync(sessionId, userId, cancellationToken);
    }

    public async Task<EntityResult> ReserveForUserAsync(Guid sessionId, string? userId, CancellationToken cancellationToken)
    {
        EntityResult<AppointmentSession> sessionResult = await GetEntityAsync(sessionId, cancellationToken);

        if (sessionResult.IsFailure || sessionResult.Value is null)
        {
            EntityError[] errors = sessionResult.Errors.Count > 0
                ? [.. sessionResult.Errors]
                : [EntityError.NotFound("Session not found.")];
            return EntityResult.Failure(errors);
        }

        AppointmentSession session = sessionResult.Value;

        if (session.Status != AppointmentSessionStatus.Open)
        {
            return EntityResult.Failure(EntityError.Conflict("Session is not open for reservations."));
        }

        if (session.IsFull())
        {
            return EntityResult.Failure(EntityError.Conflict("Session is at full capacity."));
        }

        await Mediator.Send(new ReserveSessionCommand(sessionId, userId), cancellationToken);
        return EntityResult.Success();
    }

    public async Task<EntityResult> CancelReservationAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        string? userId = requestContext.UserId;
        if (userId is null)
        {
            return EntityResult.Failure(EntityError.Unknown("User is not authenticated."));
        }

        await Mediator.Send(new CancelSessionReservationCommand(sessionId, userId), cancellationToken);
        return EntityResult.Success();
    }
}
