using Vitarish.Appointments.Application.Events;
using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Appointments.Infrastructure.Services;
using Vitarish.Requests.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class MoveReservationHandler(
    AppointmentsDbContext dbContext,
    SessionBookingPolicyEvaluator policyEvaluator,
    IAppointmentsEventPublisher eventPublisher,
    IRequestContext requestContext)
    : IRequestHandler<MoveReservationCommand>
{
    public async Task Handle(MoveReservationCommand request, CancellationToken cancellationToken)
    {
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            Reservation? original = await dbContext.Reservations
                .FirstOrDefaultAsync(r => r.Id == request.ReservationId, cancellationToken);

            if (original is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            Session? targetSession = await dbContext.Sessions
                .AsNoTracking()
                .Include(s => s.BookingPolicy)
                .FirstOrDefaultAsync(s => s.Id == request.TargetSessionId, cancellationToken);

            if (targetSession is null || targetSession.Status != SessionStatus.Active)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            if (!policyEvaluator.IsBookingOpen(targetSession, request.TargetDate))
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            Guid fromSessionId = original.SessionId;
            DateOnly fromDate = original.Date;

            original.Status = ReservationStatus.Cancelled;
            original.CancelledAt = DateTimeOffset.UtcNow;

            string? tenantId = requestContext.TenantId;
            SessionReservationLock? lockEntry = tenantId is not null
                ? new() { SessionId = request.TargetSessionId, Date = request.TargetDate, TenantId = tenantId }
                : null;

            if (lockEntry is not null)
            {
                dbContext.SessionReservationLocks.Add(lockEntry);
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException lockEx) when (AppointmentsDatabaseExceptions.IsUniqueConstraintViolation(lockEx))
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            int seats = await dbContext.Reservations.CountAsync(r =>
                r.SessionId == request.TargetSessionId &&
                r.Date == request.TargetDate &&
                r.Status != ReservationStatus.Cancelled &&
                r.Status != ReservationStatus.Rejected &&
                r.Status != ReservationStatus.Expired &&
                !r.IsHold,
                cancellationToken);

            if (seats >= targetSession.TotalSeats)
            {
                if (lockEntry is not null)
                {
                    dbContext.SessionReservationLocks.Remove(lockEntry);
                }
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            Reservation newReservation = new()
            {
                SessionId = request.TargetSessionId,
                Date = request.TargetDate,
                ReservingPartyId = original.ReservingPartyId,
                Status = ReservationStatus.Pending
            };

            dbContext.Reservations.Add(newReservation);

            if (lockEntry is not null)
            {
                dbContext.SessionReservationLocks.Remove(lockEntry);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await eventPublisher.PublishReservationMovedAsync(
                new ReservationMovedEvent(
                    original.Id,
                    fromSessionId,
                    fromDate,
                    newReservation.Id,
                    request.TargetSessionId,
                    request.TargetDate,
                    original.ReservingPartyId),
                cancellationToken);
        });
    }
}
