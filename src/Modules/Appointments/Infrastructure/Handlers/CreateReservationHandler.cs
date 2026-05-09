using System.Data;
using Vitarish.Appointments.Application.Events;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class CreateReservationHandler(
    AppointmentsDbContext dbContext,
    IBookingPolicyEngine bookingPolicyEngine,
    ICapacityEngine capacityEngine,
    IAppointmentsEventPublisher eventPublisher,
    IPublisher publisher)
    : IRequestHandler<CreateReservationCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        EntityResult<SessionOccurrence> occurrenceResult = await ResolveOccurrenceAsync(request, cancellationToken);
        if (occurrenceResult.IsFailure || occurrenceResult.Value is null)
        {
            return occurrenceResult.IsFailure
                ? EntityResult<Guid>.Failure([.. occurrenceResult.Errors])
                : EntityResult<Guid>.Failure(EntityError.Conflict("Session occurrence could not be resolved."));
        }

        SessionOccurrence occurrence = occurrenceResult.Value;
        EntityResult validationResult = await ValidateCapacityAsync(occurrence, request.ReservingPartyId, cancellationToken);
        if (validationResult.IsFailure)
        {
            return EntityResult<Guid>.Failure([.. validationResult.Errors]);
        }

        Reservation reservation = new()
        {
            SessionOccurrenceId = occurrence.Id,
            ReservingPartyId = request.ReservingPartyId,
            Status = ReservationStatus.Pending,
            IsHold = request.IsHold,
            ExpiresAt = request.IsHold && request.HoldDuration.HasValue
                ? DateTimeOffset.UtcNow.Add(request.HoldDuration.Value)
                : null
        };

        try
        {
            _ = await dbContext.Reservations.AddAsync(reservation, cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return EntityResult<Guid>.Failure(EntityError.Concurrency("Reservation capacity changed while creating the reservation. Please try again."));
        }
        catch (DbUpdateException)
        {
            return EntityResult<Guid>.Failure(EntityError.Conflict("Reservation could not be created because the booking state changed."));
        }

        await eventPublisher.PublishReservationCreatedAsync(
            new ReservationCreatedEvent(reservation.Id, reservation.SessionOccurrenceId, reservation.ReservingPartyId),
            cancellationToken);

        await publisher.Publish(
            new SessionReminderPolicyTriggeredEvent(
                reservation.Id,
                reservation.SessionOccurrenceId,
                reservation.ReservingPartyId,
                ReservationStatus.Pending,
                new DateTimeOffset(occurrence.Date.ToDateTime(occurrence.StartTime), TimeSpan.Zero)),
            cancellationToken);

        return EntityResult<Guid>.Success(reservation.Id);
    }

    private async Task<EntityResult<SessionOccurrence>> ResolveOccurrenceAsync(
        CreateReservationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.SessionOccurrenceId is { } existingOccurrenceId)
        {
            SessionOccurrence? existingOccurrence = await dbContext.SessionOccurrences
                .Include(o => o.Session)
                .FirstOrDefaultAsync(o => o.Id == existingOccurrenceId, cancellationToken);

            if (existingOccurrence is null)
            {
                return EntityResult<SessionOccurrence>.Failure(EntityError.NotFound("Session occurrence not found.", nameof(request.SessionOccurrenceId)));
            }

            if (existingOccurrence.SessionId != request.SessionId || existingOccurrence.Date != request.Date)
            {
                return EntityResult<SessionOccurrence>.Failure(EntityError.Validation(
                    "Session occurrence does not match the requested session and date.",
                    nameof(request.SessionOccurrenceId)));
            }

            return EntityResult<SessionOccurrence>.Success(existingOccurrence);
        }

        if (request.SessionId == Guid.Empty)
        {
            return EntityResult<SessionOccurrence>.Failure(EntityError.Validation("Session is required.", new Dictionary<string, string>
            {
                [nameof(request.SessionId)] = "Session is required."
            }));
        }

        Session? session = await dbContext.Sessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
        {
            return EntityResult<SessionOccurrence>.Failure(EntityError.NotFound("Session not found.", nameof(request.SessionId)));
        }

        EntityResult sessionDateResult = ValidateSessionDate(session, request.Date);
        if (sessionDateResult.IsFailure)
        {
            return EntityResult<SessionOccurrence>.Failure([.. sessionDateResult.Errors]);
        }

        SessionOccurrence? existing = await dbContext.SessionOccurrences
            .Include(o => o.Session)
            .FirstOrDefaultAsync(o => o.SessionId == request.SessionId && o.Date == request.Date, cancellationToken);

        if (existing is not null)
        {
            return EntityResult<SessionOccurrence>.Success(existing);
        }

        SessionOccurrence occurrence = CreateOccurrence(session, request.Date);
        _ = dbContext.SessionOccurrences.Add(occurrence);

        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            occurrence.Session = session;
            return EntityResult<SessionOccurrence>.Success(occurrence);
        }
        catch (DbUpdateException exception) when (IsDuplicateSessionOccurrenceException(exception))
        {
            dbContext.Entry(occurrence).State = EntityState.Detached;

            SessionOccurrence? concurrentOccurrence = await dbContext.SessionOccurrences
                .Include(o => o.Session)
                .FirstOrDefaultAsync(o => o.SessionId == request.SessionId && o.Date == request.Date, cancellationToken);

            return concurrentOccurrence is null
                ? EntityResult<SessionOccurrence>.Failure(EntityError.Conflict("Session occurrence could not be created."))
                : EntityResult<SessionOccurrence>.Success(concurrentOccurrence);
        }
    }

    private async Task<EntityResult> ValidateCapacityAsync(
        SessionOccurrence occurrence,
        string? reservingPartyId,
        CancellationToken cancellationToken)
    {
        if (occurrence.Session is null)
        {
            return EntityResult.Failure(EntityError.NotFound("Session not found.", nameof(occurrence.SessionId)));
        }

        if (occurrence.Session.Status != SessionStatus.Active)
        {
            return EntityResult.Failure(EntityError.Conflict("This session is not open for reservations."));
        }

        if (occurrence.IsCancelled)
        {
            return EntityResult.Failure(EntityError.Conflict("This session occurrence is not open for reservations."));
        }

        if (!await bookingPolicyEngine.IsBookingOpenAsync(occurrence.Id, cancellationToken))
        {
            return EntityResult.Failure(EntityError.Conflict("The booking window is closed for this session occurrence."));
        }

        if (!await bookingPolicyEngine.HasCapacityForPartyAsync(occurrence.Id, reservingPartyId, cancellationToken))
        {
            return EntityResult.Failure(EntityError.Conflict("The reservation limit for this party has been reached."));
        }

        if (await capacityEngine.GetAvailableSeatsAsync(occurrence.Id, cancellationToken) <= 0)
        {
            return EntityResult.Failure(EntityError.Conflict("No seats are available for this session occurrence."));
        }

        return EntityResult.Success();
    }

    private static EntityResult ValidateSessionDate(Session session, DateOnly date)
    {
        if (session.Status != SessionStatus.Active)
        {
            return EntityResult.Failure(EntityError.Conflict("This session is not open for reservations."));
        }

        if (date.DayOfWeek != session.DayOfWeek)
        {
            return EntityResult.Failure(EntityError.Validation("Occurrence date must match the session day of week.", new Dictionary<string, string>
            {
                [nameof(date)] = "Occurrence date must match the session day of week."
            }));
        }

        return EntityResult.Success();
    }

    private static SessionOccurrence CreateOccurrence(Session session, DateOnly date) => new()
    {
        SessionId = session.Id,
        Date = date,
        StartTime = session.StartTime,
        EndTime = session.EndTime,
        Capacity = session.TotalSeats,
        Metadata = new Dictionary<string, object?>(session.Metadata),
        IsCancelled = false
    };

    private static bool IsDuplicateSessionOccurrenceException(DbUpdateException exception)
    {
        string message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("IX_SessionOccurrences_SessionId_Date", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }
}
