using Vitarish.Appointments.Application.Events;
using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class CreateReservationHandler(
    AppointmentsDbContext dbContext,
    ISessionBookingPolicy bookingPolicy,
    IAppointmentsEventPublisher eventPublisher,
    IPublisher publisher,
    IRequestContext requestContext)
    : IRequestHandler<CreateReservationCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        IExecutionStrategy executionStrategy = dbContext.Database.CreateExecutionStrategy();
        EntityResult<Reservation> createResult = await executionStrategy.ExecuteAsync(
            async () =>
            {
                await using IDbContextTransaction transaction = await dbContext.Database
                    .BeginTransactionAsync(cancellationToken);

                Session? session = await dbContext.Sessions
                    .AsNoTracking()
                    .Include(s => s.BookingPolicy)
                    .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

                EntityResult sessionValidation = ValidateSession(session, request.Date);
                if (sessionValidation.IsFailure)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return EntityResult<Reservation>.Failure([.. sessionValidation.Errors]);
                }

                if (!await bookingPolicy.IsBookingOpenAsync(request.SessionId, request.Date, cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return EntityResult<Reservation>.Failure(EntityError.Conflict("The booking window is closed for this session slot."));
                }

                if (!await bookingPolicy.HasCapacityForPartyAsync(request.SessionId, request.Date, request.ReservingPartyId, cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return EntityResult<Reservation>.Failure(EntityError.Conflict("The reservation limit for this party has been reached."));
                }

                string? tenantId = requestContext.TenantId;

                if (tenantId is not null)
                {
                    SessionReservationLock lockEntry = new()
                    {
                        SessionId = request.SessionId,
                        Date = request.Date,
                        TenantId = tenantId
                    };

                    dbContext.SessionReservationLocks.Add(lockEntry);
                    try
                    {
                        await dbContext.SaveChangesAsync(cancellationToken);
                    }
                    catch (DbUpdateException lockEx) when (AppointmentsDatabaseExceptions.IsUniqueConstraintViolation(lockEx))
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return EntityResult<Reservation>.Failure(EntityError.Concurrency("This slot is temporarily locked. Please try again."));
                    }

                    int occupiedSeats = await CountActiveReservationsAsync(request.SessionId, request.Date, cancellationToken);
                    if (occupiedSeats >= session!.TotalSeats)
                    {
                        dbContext.SessionReservationLocks.Remove(lockEntry);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.RollbackAsync(cancellationToken);
                        return EntityResult<Reservation>.Failure(EntityError.Conflict("No seats are available for this session slot."));
                    }

                    Reservation reservation = BuildReservation(request);

                    dbContext.Reservations.Add(reservation);
                    dbContext.SessionReservationLocks.Remove(lockEntry);

                    try
                    {
                        await dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        return EntityResult<Reservation>.Success(reservation);
                    }
                    catch (DbUpdateException)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return EntityResult<Reservation>.Failure(EntityError.Conflict("Reservation could not be created."));
                    }
                }

                // Fallback path (no tenant, e.g. test environment without multi-tenancy)
                int seats = await CountActiveReservationsAsync(request.SessionId, request.Date, cancellationToken);
                if (seats >= session!.TotalSeats)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return EntityResult<Reservation>.Failure(EntityError.Conflict("No seats are available for this session slot."));
                }

                Reservation fallbackReservation = BuildReservation(request);
                dbContext.Reservations.Add(fallbackReservation);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return EntityResult<Reservation>.Success(fallbackReservation);
            });

        if (createResult.IsFailure)
        {
            return EntityResult<Guid>.Failure([.. createResult.Errors]);
        }

        Reservation created = createResult.Value;

        await eventPublisher.PublishReservationCreatedAsync(
            new ReservationCreatedEvent(created.Id, created.SessionId, created.Date, created.ReservingPartyId),
            cancellationToken);

        await publisher.Publish(
            new SessionReminderPolicyTriggeredEvent(
                created.Id,
                created.SessionId,
                created.Date,
                created.ReservingPartyId,
                ReservationStatus.Pending),
            cancellationToken);

        return EntityResult<Guid>.Success(created.Id);
    }

    private static EntityResult ValidateSession(Session? session, DateOnly date)
    {
        if (session is null)
        {
            return EntityResult.Failure(EntityError.NotFound("Session not found.", "sessionId"));
        }

        if (session.Status != SessionStatus.Active)
        {
            return EntityResult.Failure(EntityError.Conflict("This session is not open for reservations."));
        }

        if (date.DayOfWeek != session.DayOfWeek)
        {
            return EntityResult.Failure(EntityError.Validation("Reservation date must match the session day of week.", new Dictionary<string, string>
            {
                ["date"] = "Reservation date must match the session day of week."
            }));
        }

        if (session.BookingPolicy?.ClosedDates.Any(cd => cd.Date == date) == true)
        {
            return EntityResult.Failure(EntityError.Conflict("This date is closed for reservations."));
        }

        return EntityResult.Success();
    }

    private static Reservation BuildReservation(CreateReservationCommand request) => new()
    {
        SessionId = request.SessionId,
        Date = request.Date,
        ReservingPartyId = request.ReservingPartyId,
        Status = ReservationStatus.Pending,
        IsHold = request.IsHold,
        ExpiresAt = request.IsHold && request.HoldDuration.HasValue
            ? DateTimeOffset.UtcNow.Add(request.HoldDuration.Value)
            : null
    };

    private Task<int> CountActiveReservationsAsync(Guid sessionId, DateOnly date, CancellationToken cancellationToken) =>
        dbContext.Reservations.CountAsync(r =>
            r.SessionId == sessionId &&
            r.Date == date &&
            r.Status != ReservationStatus.Cancelled &&
            r.Status != ReservationStatus.Rejected &&
            r.Status != ReservationStatus.Expired &&
            !r.IsHold,
            cancellationToken);
}
